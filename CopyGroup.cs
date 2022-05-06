using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
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
            UIDocument uiDoc = commandData.Application.ActiveUIDocument; //  - получили доступ к документу 
            Document doc = uiDoc.Document; // получаем ссылку на экз-р классса Document, котый будет содержать
                                           // базу данных эл-тов внутри окрытого документа

            Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, "Выберите группу объектов");
            //попросить пользователя выбрать группу для копирования
            // получили ссылку на выбранную пользователем группу объектов
            //то что связано с выбором сгруппировано в объект Selection
            //ObjectType это перечисление используется только в случае выбра Pick
            //результатом будет объект типа Reference
            Element element = doc.GetElement(reference);//ссылку reference указываем в качестве аргумента,
                                                        //как резельтат получаем объект типа Element

            Group group = element as Group; // тот объект по которому пользователь щелкнул получили и преобразовали к типу Group
                                            // ключевое слово AS
            XYZ point = uiDoc.Selection.PickPoint("Выберите точку"); //попросим пользователя выбрать какую-то точку,
                                                                     //это будет комната в которую мы хотим скопировать объект 

            Transaction transaction = new Transaction(doc);//транзакция создается припомощи конструктора,
            // в качестве конструктора передаем ссылку на документ для которого выполняется изменение

            transaction.Start("Копирование группы объектов");//открываем транзакцию в качестве аргумента указываем сообщение
            doc.Create.PlaceGroup(point, group.GroupType); //перечисляем действия которые должны изменить нашу модель
            // обращаемся к документу doc(база данных в моделе) к его свойству Create которое и вызвать у него метод PlaceGroup( в качестве
            //аргумента передаем точку "point", из group получаем GroupType

            transaction.Commit(); //после того как все методы выполнены закрываем транзакцию (подтверждаем изменения)

            return Result.Succeeded;

        }
    }
}

//ссылка это не сам объект, ссылка это что-то типа идентификатора объекта
//если мы хотим скопировать (изменить и т.д.) то должны иметь именно сам объект, ссылки недостаточно нужен доступ к самому объекту
// получить его можно при помощи GetElement
//Element это родительский или базовый класс для всех элементов RevitAPI (например стены будут унаследованы от класса Element)
//XYZ так определяется "точка" в RevitAPI
