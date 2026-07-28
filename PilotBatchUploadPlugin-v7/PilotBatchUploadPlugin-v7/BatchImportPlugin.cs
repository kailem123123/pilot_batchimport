using Ascon.Pilot.SDK;
using Ascon.Pilot.SDK.Menu;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PilotSimpleImportPlugin
{
    [Export(typeof(IMenu<ObjectsViewContext>))]
    public class BatchImportPlugin : IMenu<ObjectsViewContext>
    {
        private const string CommandName = "cmd_batch_import";

        private readonly IObjectsRepository _repository;
        private readonly IObjectModifier _modifier;

        [ImportingConstructor]
        public BatchImportPlugin(IObjectsRepository repository, IObjectModifier modifier)
        {
            _repository = repository;
            _modifier = modifier;
        }

        public void Build(IMenuBuilder builder, ObjectsViewContext context)
        {
            if (context == null || context.SelectedObjects == null || !context.SelectedObjects.Any())
                return;

            builder.AddItem(CommandName, 0)
                   .WithHeader("Пакетный импорт исходных файлов");
        }

        public void OnMenuItemClick(string name, ObjectsViewContext context)
        {
            if (name != CommandName)
                return;

            Ascon.Pilot.SDK.IDataObject parentFolder = context.SelectedObjects.FirstOrDefault();
            if (parentFolder == null)
            {
                MessageBox.Show(
                    "Не выбрана папка-приёмник в дереве Pilot!",
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Multiselect = true;
                openFileDialog.Filter = "Все файлы (*.*)|*.*";
                openFileDialog.Title = "Выберите файлы для импорта как исходники";

                if (openFileDialog.ShowDialog() != DialogResult.OK)
                    return;

                // Поиск типов данных
                var documentType = _repository.GetTypes().FirstOrDefault(t => t.Name == "document")
                               ?? _repository.GetTypes().FirstOrDefault(t => t.Name.ToLower().Contains("doc"));

                var sourceFileType = _repository.GetType(SystemTypeNames.PROJECT_FILE);

                if (documentType == null || sourceFileType == null)
                {
                    MessageBox.Show(
                        "Необходимые типы объектов (document/File) не найдены в конфигурации сервера!",
                        "Ошибка конфигурации",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                int counter = 0;
                int errorCounter = 0;

                foreach (string filePath in openFileDialog.FileNames)
                {
                    FileStream fs = null;
                    try
                    {
                        string fileName = Path.GetFileName(filePath);
                        string nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);

                        DateTime creationTime = File.GetCreationTime(filePath);
                        DateTime lastAccessTime = File.GetLastAccessTime(filePath);
                        DateTime lastWriteTime = File.GetLastWriteTime(filePath);

                     
                        var documentBuilder = _modifier.Create(parentFolder, documentType);

                        if (documentType.Attributes.Any(a => a.Name == "title"))
                            documentBuilder.SetAttribute("title", nameWithoutExt);

                        if (documentType.Attributes.Any(a => a.Name == "name"))
                            documentBuilder.SetAttribute("name", nameWithoutExt);

                        documentBuilder.SetAttribute("object_name", nameWithoutExt);

                        
                        var sourceFileBuilder = _modifier.Create(documentBuilder.DataObject, sourceFileType);
                        sourceFileBuilder.SetAttribute(SystemAttributeNames.PROJECT_ITEM_NAME, fileName);

                        // Читаем байты нативного файла и пишем в созданный дочерний File-контейнер
                        fs = File.OpenRead(filePath);
                        sourceFileBuilder.AddFile(fileName, fs, creationTime, lastAccessTime, lastWriteTime);

                        // ── ШАГ 3: Линкуем связь "Исходный файл" для боковой панели ──────────────
                        // Это заполняет IDataObject.RelatedSourceFiles и связывает их логически
                        documentBuilder.AddRelation(
                            name: string.Empty,
                            type: ObjectRelationType.SourceFiles,
                            sourceId: Guid.Empty, // Сервер автоматически подставит ID текущего документа
                            targetId: sourceFileBuilder.DataObject.Id, // ID нашего дочернего файла
                            versionId: DateTime.MinValue);

                        // Apply() один раз на итерацию фиксирует созданное дерево: Папка -> Документ -> Файл
                        _modifier.Apply();

                        counter++;
                    }
                    catch (Exception ex)
                    {
                        errorCounter++;
                        _modifier.Clear();

                        MessageBox.Show(
                            $"Ошибка при импорте файла:\n{filePath}\n\n{ex.Message}",
                            "Ошибка",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                    finally
                    {
                        fs?.Dispose();
                    }
                }

                // Вывод результатов
                if (counter > 0 && errorCounter == 0)
                {
                    MessageBox.Show($"Импорт завершен! Создано документов с исходниками: {counter}", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}