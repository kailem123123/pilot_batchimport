using Ascon.Pilot.SDK;
using PilotBatchImport;
using System.ComponentModel.Composition;

namespace PilotSimpleImportPlugin
{
    [Export(typeof(IPilotExtension))]
    public class EntryPoint : IPilotExtension
    {
        [ImportingConstructor]
        public EntryPoint()
        {
        }

        public void Initialize()
        {
            // Здесь пусто. MEF сам подхватит и встроит наше меню 
            // благодаря атрибуту [Export] в файле BatchImportPlugin.cs
        }

        public void Shutdown()
        {
        }
    }
}