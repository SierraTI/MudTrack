using ProjectReport.Modules.VolumeBalance.Models;
using System;
using System.Collections.ObjectModel;

namespace ProjectReport.Modules.VolumeBalance.Services
{
    public class AdditionsChemicalService
    {
        // =====================================================
        // SINGLETON
        // =====================================================

        private static readonly Lazy<AdditionsChemicalService> _instance =
            new(() => new AdditionsChemicalService());

        public static AdditionsChemicalService Instance =>
            _instance.Value;


        // =====================================================
        // CONSTRUCTOR PRIVADO
        // =====================================================

        private AdditionsChemicalService()
        {
        }


        // =====================================================
        // LIVE COLLECTION
        // Colección principal en memoria
        // =====================================================

        public ObservableCollection<AdditionsChemicalVol>
            LiveAdditionsChemicalVolumes
        { get; }
            = new ObservableCollection<AdditionsChemicalVol>();


        // =====================================================
        // EVENT
        // Notifica cambios en la colección
        // =====================================================

        public event EventHandler? AdditionsChemicalUpdated;


        // =====================================================
        // SAVE
        // Guarda los registros en memoria
        // =====================================================

        public void SaveAdditions(
            ObservableCollection<AdditionsChemicalVol> additions)
        {
            // Limpiar registros actuales
            LiveAdditionsChemicalVolumes.Clear();


            // Copiar nuevos registros
            foreach (var item in additions)
            {
                LiveAdditionsChemicalVolumes.Add(item);
            }


            // Notificar actualización
            AdditionsChemicalUpdated?.Invoke(
                this,
                EventArgs.Empty);
        }


        // =====================================================
        // ADD
        // Agregar un registro a memoria
        // =====================================================

        public void AddAddition(
            AdditionsChemicalVol addition)
        {
            if (addition == null)
                return;


            LiveAdditionsChemicalVolumes.Add(addition);


            AdditionsChemicalUpdated?.Invoke(
                this,
                EventArgs.Empty);
        }


        // =====================================================
        // REMOVE
        // Eliminar un registro de memoria
        // =====================================================

        public void RemoveAddition(
            AdditionsChemicalVol addition)
        {
            if (addition == null)
                return;


            if (!LiveAdditionsChemicalVolumes.Contains(addition))
                return;


            LiveAdditionsChemicalVolumes.Remove(addition);


            AdditionsChemicalUpdated?.Invoke(
                this,
                EventArgs.Empty);
        }


        // =====================================================
        // CLEAR
        // Eliminar todos los registros de memoria
        // =====================================================

        public void Clear()
        {
            LiveAdditionsChemicalVolumes.Clear();


            AdditionsChemicalUpdated?.Invoke(
                this,
                EventArgs.Empty);
        }


        // =====================================================
        // CACHE
        // Indica si existen registros en memoria
        // =====================================================

        public bool HasCacheData =>
            LiveAdditionsChemicalVolumes.Count > 0;
    }
}