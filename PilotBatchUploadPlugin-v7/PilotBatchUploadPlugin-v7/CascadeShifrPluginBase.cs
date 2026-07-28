using Ascon.Pilot.SDK;
using Ascon.Pilot.SDK.Menu;
using System;
using System.ComponentModel.Composition;
using System.Linq;

namespace PilotCascadeShifrPlugin
{
    [Export(typeof(IMenu<ObjectsViewContext>))]
    public class CascadeShifrPluginBase
    {

        private void UpdateTree(Ascon.Pilot.SDK.IDataObject projectRoot, string newShifr, ref int counter)
        {
            throw new NotImplementedException();
        }

        private void UpdateTree(Ascon.Pilot.SDK.IDataObject parent, string newShifr, ref int counter)
        {
            // Получаем все дочерние объекты
            var children = _repository.GetChildren(parent.Id).ToList();

            foreach (var child in children)
            {
                var type = _repository.GetType(child.TypeId);

                if (type == null) continue;

                // Проверяем тип объекта
                if (type.Name == "section" || type.Name == "subsection" || type.Name == "document")
                {
                    var edit = _modifier.Edit(child);

                    if (child.HasAttribute("code"))
                        edit.SetAttribute("code", newShifr);

                    if (child.DisplayName.Contains(PlaceholderShifr))
                    {
                        edit.SetAttribute("object_name", child.DisplayName.Replace(PlaceholderShifr, newShifr));
                    }

                    _modifier.Apply();
                    counter++;
                }

                // Рекурсивно обрабатываем дочерние элементы
                UpdateTree(child, newShifr, ref counter);
            }
        }
    }
}