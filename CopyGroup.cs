using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyCroupPluginLab2
{
    [TransactionAttribute(TransactionMode.Manual)] // Manual - это ручной режим, это мы вручную помечаем АТРИБУТ
                                                   //TransactionAuto то ревит сам автоматически определяет в какой момент нужна будет транзакция

    public class CopyGroup : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)// метод Execute должен
                      // возвращать значение типа Result который указывает успешно или неуспешно завершилась команда 
                      //в случае если команда завершается не успешно все созданные в ее процесе транзакции откатываются ревитом
                      //метод Execute принимает три аргумента: commandData (добираемся к ревит, к открытому документу, к базе данных открытого документа);
                      //tring message (строковое значение помеченое как REF) это значит что этот параметер передается по ссылке,
                      //а значит что мы можем изменить это сообщение и оно вернется в ревит;
                      //elements это набор элементов, которые будут подсвечены в длкументе если команда завершится неудачно.  
        {
            try       // вызываем исключение (в блок try помещаем весь метод)
            {

                UIDocument uiDoc = commandData.Application.ActiveUIDocument; //  - получили доступ к документу 
                Document doc = uiDoc.Document; // получаем ссылку на экз-р классса Document, котый будет содержать
                                               // базу данных эл-тов внутри окрытого документа
                
                GroupPickFilter groupPickFilter = new GroupPickFilter();
                //добавляем фильтр ввода в момент выбора объекта в момент перегрузки принимает ISelectionFilter
                //Для создания фильтра передаем в метод PickObject еще один аргумент вторым по счету, который будет представлять из себя 
                //экземпляр некого класса унаследованного от ISelectionFilter

                Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, groupPickFilter, "Выберите группу объектов");
                //попросить пользователя выбрать группу для копирования
                // получили ссылку на выбранную пользователем группу объектов
                //то что связано с выбором сгруппировано в объект Selection
                //ObjectType это перечисление используется только в случае выбра Pick
                //результатом будет объект типа Reference
                Element element = doc.GetElement(reference);//ссылку reference указываем в качестве аргумента,
                                                            //как резельтат получаем объект типа Element

                Group group = element as Group; // тот объект по которому пользователь щелкнул получили и преобразовали к типу Group
                                                // ключевое слово AS

                XYZ groupCenter = GetElementCenter(group);  // находим цетр группы
                Room room = GetRoomByPoint(doc, groupCenter); // в какую комнату попадает эта точка
                XYZ roomCenter = GetElementCenter(room); // находим центр комнаты
                XYZ offset = groupCenter - roomCenter; // определить смещение центра группы относительно центра комнаты


                XYZ point = uiDoc.Selection.PickPoint("Выберите точку"); //попросим пользователя выбрать какую-то точку,
                                                                         //это будет комната в которую мы хотим скопировать объект 
                Room room2 = GetRoomByPoint(doc, point); // определяем комнату по которой щелкнул пользователь
                XYZ room2Center = GetElementCenter(room2); // находим центр этой комнаты
                XYZ offset2 = room2Center + offset; // на основе смещения вычисляем точку в которой необходимо выполнить вставку группы


                Transaction transaction = new Transaction(doc);//транзакция создается припомощи конструктора,
                                                               // в качестве конструктора передаем ссылку на документ для которого выполняется изменение

                transaction.Start("Копирование группы объектов");//открываем транзакцию в качестве аргумента указываем сообщение
                doc.Create.PlaceGroup(point, group.GroupType); //перечисляем действия которые должны изменить нашу модель
                // обращаемся к документу doc(база данных в моделе) к его свойству Create которое и вызвать у него метод PlaceGroup( в качестве
                //аргумента передаем точку "point", из group получаем GroupType

                transaction.Commit(); //после того как все методы выполнены закрываем транзакцию (подтверждаем изменения)

                
            }
             catch(Autodesk.Revit.Exceptions.OperationCanceledException)
            //определяем один или несколько блоков CATCH, которые будут обрабатывать исключение (отдельно нажатие ESC)
            {
                return Result.Cancelled;  //возвращаем резельтат ОТМЕНЫ
            } 

            catch(Exception ex)
            {
                message = ex.Message; //чтобы пользователь понимал в чем проблемма передаем в "message" текст ошибки, получаем из самого 
                                      //исключения "ex"


                return Result.Failed; // в случае других ошибок возвращаем ("Failed" - не успешно)
            }

             return Result.Succeeded;
        }

        public XYZ GetElementCenter(Element element)       //Метод, который по объекту (по его элементу) вычисляет центр на основе BoundingBox
                                                           //Принимает этот метод элемент базового типа, а возвращать точку XYZ
        {
            BoundingBoxXYZ bounding = element.get_BoundingBox(null); // рамка в 3х измерениях т.е прямоугольный паралелепипед
            //В классе element определен метод get_BoundingBox, который принимает аргумент пустую ссылку null
            //Через доступ МIN и MAX можно получить доступ к минимальной и максимальной точке
            //МIN это точка левый нижний дальний угол
            //MAX это правый верхний ближний угол

            return (bounding.Max + bounding.Min) / 2; // находим центр (возвращает значения центра, центральной точки)
        }

        public Room GetRoomByPoint(Document doc, XYZ point) // метод должен определять комнату поисходной точке, аргументом есть точка XYZ по которой
                                                            // осуществ. поиск
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);// для поиска создаем класс FilteredElementCollector
            //Аргументом конструктора является ссылка на документ в котором производится поиск

            collector.OfCategory(BuiltInCategory.OST_Rooms);// выполняем поиск на основе быстрого фильтра OfCategory по категории OST_Rooms
            //отбераем в документе все комнаты
            foreach (Element e in collector) //перебираем все содержимое фильтра
            {
                Room room = e as Room; //объект приводим к типу комнаты

                if (room != null) //если комната не ровна null
                {
                    if (room.IsPointInRoom(point)) //метод IsPointInRoom проверяет содержит ли комната точку
                    {
                        return room;
                    }
                }
            }
            return null;// если не найдем точку вернем null
        }

    }
    public class GroupPickFilter : ISelectionFilter //создаем класс для фильтра унаследован от ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_IOSModelGroups) // если эл-т группа - true
                return true;
            else  // если эл-т что то дргугое  - false
                return false;
            //для элемента возвращаем либо try либо false в зависимости от типа элемента
            //если элемент является группой мы должны вернуть TRY, если является чем-то другим вернуть FALSE
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false; //не зависимо какая это ссылка для нее всегда будет возвращаться "ложь"
        }
    }
}

//ссылка это не сам объект, ссылка это что-то типа идентификатора объекта
//если мы хотим скопировать (изменить и т.д.) то должны иметь именно сам объект, ссылки недостаточно нужен доступ к самому объекту
// получить его можно при помощи GetElement
//Element это родительский или базовый класс для всех элементов RevitAPI (например стены будут унаследованы от класса Element)
//XYZ так определяется "точка" в RevitAPI

//Можно делать один блок CATCH, но в даном примере сделаем два : один - отдельно обработаем исключение связанное с нажатием Esc
// второй (отдельно) все остальные ошибки, которые могли произойти в процессе работы
//В RevitAPI определены собственные исключения добраться к ним можно через (Autodes.kRevit.Exceptions.OperationCanceledException)
//Исключение связанное с нажатием кнопки отмены называется "OperationCanceledException"
