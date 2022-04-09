using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAPI_FinalPlugin
{
    public class MainViewViewModel
    {
        #region Инициализация полей и свойств
        private ExternalCommandData _commandData;

        public List<Level> Levels { get; } = new List<Level>();
        public List<RoomTagType> RoomTags { get; } = new List<RoomTagType>();

        public DelegateCommand SaveAutoTagCommand { get; }
        public RoomTagType SelectedRoomTag { get; set; }
        public Level SelectedLevel { get; set; }
        #endregion

        #region Инициализация конструктора 
        public MainViewViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;
            Levels = LevelsUtils.GetLevels(commandData);
            RoomTags = RoomTagUtils.GetRoomTags(commandData);
            SaveAutoTagCommand = new DelegateCommand(OnSaveAutoTagCommand);
        }
        #endregion

        #region Выполнение команды "Применить" для кнопки
        private void OnSaveAutoTagCommand()
        {
            Document doc = _commandData.Application.ActiveUIDocument.Document;

            try
            {
                if (SelectedRoomTag == null)
                {
                    TaskDialog.Show("Ошибка", "Выберите тип марки помещения");
                    return;
                }

                if (SelectedLevel == null)
                {
                    TaskDialog.Show("Ошибка", "Выберите уровень");
                    return;
                }

                using (Transaction transaction = new Transaction(doc))
                {
                    transaction.Start("Tag rooms");

                    PlanTopology pt = doc.get_PlanTopology(SelectedLevel);

                    foreach (PlanCircuit pc in pt.Circuits)
                    {
                        if (!pc.IsRoomLocated)
                        {
                            Room room = doc.Create.NewRoom(null, pc);
                            room.Name = $"{SelectedLevel.Name}_{room.Number}";
                            LocationPoint locationPoint = room.Location as LocationPoint;
                            UV point = new UV(locationPoint.Point.X, locationPoint.Point.Y);
                            RoomTag newTag = doc.Create.NewRoomTag(new LinkElementId(room.Id), point, null);
                            newTag.RoomTagType = SelectedRoomTag;
                        }
                    }

                    transaction.Commit();
                }

                RaiseCloseRequest();
            }
            catch (Autodesk.Revit.Exceptions.ArgumentOutOfRangeException ex)
            {
                TaskDialog.Show("Ошибка", ex.Message);
                return;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", ex.Message);
                return;
            }
        }

        public event EventHandler CloseRequest;
        private void RaiseCloseRequest()
        {
            CloseRequest?.Invoke(this, EventArgs.Empty);
        }
        #endregion
    }
}