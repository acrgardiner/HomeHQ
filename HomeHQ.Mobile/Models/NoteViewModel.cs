using System.ComponentModel;
using HomeHQ.Entities;

namespace HomeHQ.Mobile.Models;

public class NoteViewModel : INotifyPropertyChanged
{
    public Note Model { get; }

    public NoteViewModel(Note model)
    {
        Model = model ?? new Note();
        _title = Model.Title;
        _content = Model.Content;
    }

    private string _title = string.Empty;
    public string Title
    {
        get => _title;
        set
        {
            if (_title == value)
            {
                return;
            }

            _title = value;
            Model.Title = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
        }
    }

    private string _content = string.Empty;
    public string Content
    {
        get => _content;
        set
        {
            if (_content == value)
            {
                return;
            }

            _content = value;
            Model.Content = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Content)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
