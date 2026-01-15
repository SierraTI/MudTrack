using System.Collections.ObjectModel;
using System.Windows.Input;

public class WholeFluidsViewModel : INotifyPropertyChanged
{
    public ObservableCollection<WholeFluidItem> WholeFluids { get; } = new ObservableCollection<WholeFluidItem>();

    public ICommand AddCommand { get; }

    public WholeFluidsViewModel()
    {
        AddCommand = new RelayCommand(Add);
    }

    private void Add()
    {
        var item = new WholeFluidItem { Remision = Remision, Origin = Origin, Fecha = Fecha /*...*/ };
        WholeFluids.Add(item); // NO reasignar WholeFluids
    }

    // propiedades Remision, Origin, Fecha... y INotifyPropertyChanged aquí
}