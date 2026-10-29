namespace Zen.Todo;

public enum Priority
{
    Low, Normal, High
}
public enum TaskFilter
{
    All, Active, Completed
}
public sealed class TodoItem
{
    private string _title = "";
    private bool _done;
    private Priority _priority;
    public Guid Id
    {
        get;
    }
    public string Title => _title;
    public bool IsDone => _done;
    public Priority Priority => _priority;
    public TodoItem(string title, Priority priority = Priority.Normal) : this(Guid.NewGuid(), title, false, priority) { }
    [System.Text.Json.Serialization.JsonConstructor]
    public TodoItem(Guid id, string title, bool isDone, Priority priority)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Некорректный идентификатор.");
        Id = id;
        Rename(title);
        SetPriority(priority);
        _done = isDone;
    }
    public void Rename(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Введите название задачи.");
        _title = title.Trim();
    }
    public void Complete() => _done = true;
    public void SetCompleted(bool value) => _done = value;
    public void SetPriority(Priority value)
    {
        if (!Enum.IsDefined(value))
            throw new ArgumentException("Выберите допустимый приоритет.");
        _priority = value;
    }
}
public sealed class TodoList
{
    private List<TodoItem> _items = [];
    public IReadOnlyList<TodoItem> Items => _items.AsReadOnly();
    public TodoItem Add(string title, Priority priority = Priority.Normal)
    {
        var item = new TodoItem(title, priority);
        _items.Add(item);
        return item;
    }
    public TodoItem Find(Guid id) => _items.Find(x => x.Id == id) ?? throw new ArgumentException("Выберите задачу.");
    public void Remove(Guid id) => _items.Remove(Find(id));
    public IEnumerable<TodoItem> Filter(TaskFilter filter) => filter switch
    {
        TaskFilter.All => _items.ToArray(),
        TaskFilter.Active => _items.Where(x => !x.IsDone).ToArray(),
        TaskFilter.Completed => _items.Where(x => x.IsDone).ToArray(),
        _ => throw new ArgumentException("Неизвестный фильтр.")
    };
    public IEnumerable<TodoItem> Search(string query, TaskFilter filter = TaskFilter.All) => Filter(filter).Where(x => x.Title.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase));
    public IEnumerable<TodoItem> Sort(IEnumerable<TodoItem> items) => items.OrderByDescending(x => x.Priority).ThenBy(x => x.Title, StringComparer.CurrentCultureIgnoreCase);
    public void Save(string path) => Core.Storage.Save(path, _items);
    public void Load(string path)
    {
        var loaded = File.Exists(path) ? Core.Storage.Load<List<TodoItem>>(path) : [];
        if (loaded.Any(x => x is null) || loaded.Select(x => x.Id).Distinct().Count() != loaded.Count)
            throw new InvalidDataException("Некорректные задачи в файле.");
        _items = loaded;
    }
}
