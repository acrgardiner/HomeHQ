using System.ComponentModel;
using Attribute = HomeHQ.Entities.Attribute;

namespace HomeHQ.Mobile.Models;

public class AttributeViewModel : INotifyPropertyChanged
{
    public Attribute Model { get; }

    public AttributeViewModel(Attribute model)
    {
        Model = model ?? new Attribute();
        _key = Model.Key;
        _value = Model.Value;
    }

    private string? _key;
    public string? Key
    {
        get => _key;
        set
        {
            if (_key == value)
            {
                return;
            }

            _key = value;
            Model.Key = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Key)));
        }
    }

    private string? _value;
    public string? Value
    {
        get => _value;
        set
        {
            if (_value == value)
            {
                return;
            }

            _value = value;
            Model.Value = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
