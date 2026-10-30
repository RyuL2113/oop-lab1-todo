using Zen.Ui;
namespace Zen.Todo;

public sealed class MainForm : ZenForm
{
    private readonly TodoList _list = new();
    private readonly string _path;
    private readonly TextBox _title = Input("Что хочется сделать?", 410);
    private readonly TextBox _search = Input("Поиск по названию", 410);
    private readonly ComboBox _priority = Choice(["Низкий", "Обычный", "Высокий"]);
    private readonly ComboBox _filter = Choice(["Все задачи", "Активные", "Выполненные"]);
    private readonly CheckBox _sort = new() { Text = "По приоритету", AutoSize = true, Padding = new Padding(0, 5, 0, 0) };
    private readonly ListView _view = new() { Width = 970, Height = 350, View = View.Details, FullRowSelect = true, MultiSelect = false, CheckBoxes = true, BorderStyle = BorderStyle.None, BackColor = Paper, HideSelection = false };
    private readonly Label _summary = Caption("");
    private bool _refreshing;
    public MainForm(string? path = null) : base("Пространство для дел", "01 / СПИСОК ДЕЛ")
    {
        _path = path ?? Core.Storage.DataPath("tasks");
        _priority.SelectedIndex = 1;
        _view.Columns.Add("Задача", 715);
        _view.Columns.Add("Приоритет", 190);
        Body.Controls.Add(Row(_title, _priority, Button("Добавить", Add, true)));
        Body.Controls.Add(Row(_search, _filter, _sort));
        Body.Controls.Add(_summary);
        Body.Controls.Add(_view);
        Body.Controls.Add(Row(Button("Переименовать", () => { var item = Selected(); var title = Prompt("Название задачи", item.Title); if (title != null) { item.Rename(title); SaveRefresh(); } }),
            Button("Приоритет", () => { Selected().SetPriority((Priority)_priority.SelectedIndex); SaveRefresh(); }),
            Button("Удалить", () => { _list.Remove(Selected().Id); SaveRefresh(); })));
        _title.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Guard(Add); } };
        _view.ItemChecked += (_, e) => { if (!_refreshing && e.Item.Tag is TodoItem item) Guard(() => { item.SetCompleted(e.Item.Checked); SaveRefresh(); }); };
        _search.TextChanged += (_, _) => RefreshItems();
        _filter.SelectedIndexChanged += (_, _) => RefreshItems();
        _sort.CheckedChanged += (_, _) => RefreshItems();
        Shown += (_, _) => Guard(() => { _list.Load(_path); RefreshItems(); });
    }
    private TodoItem Selected() => _view.SelectedItems.Count > 0 ? (TodoItem)_view.SelectedItems[0].Tag! : throw new ArgumentException("Сначала выберите задачу.");
    private void Add()
    {
        _list.Add(_title.Text, (Priority)_priority.SelectedIndex);
        SaveRefresh();
        _title.Clear();
        _title.Focus();
    }
    private void SaveRefresh()
    {
        RefreshItems();
        _list.Save(_path);
    }
    private void RefreshItems()
    {
        _refreshing = true;
        _view.BeginUpdate();
        _view.Items.Clear();
        var items = _list.Search(_search.Text, (TaskFilter)_filter.SelectedIndex);
        if (_sort.Checked)
            items = _list.Sort(items);
        foreach (var task in items)
        {
            var item = new ListViewItem(task.Title) { Tag = task, Checked = task.IsDone, ForeColor = task.IsDone ? Matcha : task.Priority == Priority.High ? Red : Ink };
            item.SubItems.Add(new[] { "Низкий", "Обычный", "Высокий" }[(int)task.Priority]);
            _view.Items.Add(item);
        }
        _summary.Text = $"{_list.Items.Count(x => !x.IsDone):00} АКТИВНЫХ     /     {_list.Items.Count:00} ВСЕГО" + (_list.Items.Count == 0 ? "     ·     Начните с одного небольшого дела." : "");
        _view.EndUpdate();
        _refreshing = false;
    }
}
