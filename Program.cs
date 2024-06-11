using NSwag.AspNetCore; 
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args); 
builder.Services.AddDbContext<TodoDb>(opt => opt.UseInMemoryDatabase("TodoList"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddEndpointsApiExplorer();
// generación de documentación OpenAPI.
builder.Services.AddOpenApiDocument(config =>
{
  config.DocumentName = "TodoAPI"; 
  config.Title = "TodoAPI v1"; 
  config.Version = "v1"; 
});

var app = builder.Build();

// Si el entorno es de desarrollo
if (app.Environment.IsDevelopment()) 
{
  app.UseOpenApi(); 
  // Configura Swagger UI.
  app.UseSwaggerUi(config => 
  {
    config.DocumentTitle = "TodoAPI";
    config.Path = "/swagger";
    config.DocumentPath = "/swagger/{documentName}/swagger.json";
    config.DocExpansion = "list";
  });
}

RouteGroupBuilder todoItems = app.MapGroup("/todoitems");

// Define los endpoints para las operaciones CRUD.
todoItems.MapGet("/", GetAllTodos);
todoItems.MapGet("/complete", GetCompleteTodos);
todoItems.MapGet("/{id}", GetTodo);
todoItems.MapPost("/", CreateTodo);
todoItems.MapPut("/{id}", UpdateTodo);
todoItems.MapDelete("/{id}", DeleteTodo);

app.Run();

static async Task<IResult> GetAllTodos(TodoDb db)
{
  // Retorna todos los items de la base de datos en formato DTO.
  return TypedResults.Ok(await db.Todos.Select(x => new TodoItemDTO(x)).ToArrayAsync());
}

static async Task<IResult> GetCompleteTodos(TodoDb db) {
  // Retorna todos los items completos (IsComplete = true) de la base de datos en formato DTO.
  return TypedResults.Ok(await db.Todos.Where(t => t.IsComplete).Select(x => new TodoItemDTO(x)).ToListAsync());
}

static async Task<IResult> GetTodo(int id, TodoDb db)
{
  // Busca un item por ID. Si se encuentra, lo retorna en formato DTO, de lo contrario, retorna NotFound.
  return await db.Todos.FindAsync(id)
      is Todo todo
          ? TypedResults.Ok(new TodoItemDTO(todo))
          : TypedResults.NotFound();
}

static async Task<IResult> CreateTodo(TodoItemDTO todoItemDTO, TodoDb db)
{
  // Crea un nuevo item en la base de datos a partir del DTO recibido.
  var todoItem = new Todo
  {
    IsComplete = todoItemDTO.IsComplete,
    Name = todoItemDTO.Name
  };

  db.Todos.Add(todoItem);
  await db.SaveChangesAsync();

  todoItemDTO = new TodoItemDTO(todoItem);

  return TypedResults.Created($"/todoitems/{todoItem.Id}", todoItemDTO);
}

static async Task<IResult> UpdateTodo(int id, TodoItemDTO todoItemDTO, TodoDb db)
{
  // Busca un item por ID y actualiza sus valores a partir del DTO recibido.
  var todo = await db.Todos.FindAsync(id);

  if (todo is null) return TypedResults.NotFound();

  todo.Name = todoItemDTO.Name;
  todo.IsComplete = todoItemDTO.IsComplete;

  await db.SaveChangesAsync();

  return TypedResults.NoContent();
}

static async Task<IResult> DeleteTodo(int id, TodoDb db)
{
  // Busca un item por ID y lo elimina si se encuentra.
  if (await db.Todos.FindAsync(id) is Todo todo)
  {
    db.Todos.Remove(todo);
    await db.SaveChangesAsync();
    return TypedResults.NoContent();
  }

  return TypedResults.NotFound();
}
