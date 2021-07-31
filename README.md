# FAT

Во время проектирования файловой системы была спроектирована логическая разметка диска, которая представленна ниже.

| Главная запись | Файл MFT | Файл ASFS | Файл GROUPS | Файл USERS | Блоки данных | Копия файла | MFT Блоки данных | Копия главной записи |
| -------------- | -------- | --------- | ----------- | ---------- | ------------ | ----------- | ---------------- | -------------------- |

#### Главная запись

Главная запись – запись, которая используется при старте системы и
предназначена для сохранения информации.
При запуске системы данные из главной записи получают номера адресов блоков MFT, копии MFT, размер кластера. После получения этих данных происходит проверка на целостность считанных данных с главной записи. Для целостности полученных данных используется формула:

CRC = (Размер сектора - Номер блока MFT) / 3

Если полученные данные по формуле не равны, считается, что данные в MFT повреждены. В этом случае считываются данные с копии MFT и проверяется значение новых данных по той же формуле. В случае неудачи, файловая система возвращает ошибку и заканчивает своё выполнение.

Структура главной записи представлена в таблице ниже.

| Название поля | Тип | Размерность | Описание |
| ------------- | --- | ----------- | -------- |
| SectorSize | uint | 4Б | Размер блока. Предназначен для определения размера блока, который используется в ФС. |
| Num_Block | uint | 4Б | Номер блока MFT. Указывает на первый блок, где хранятся данные файла MFT. |
| Num_Copy_MFT | uint | 4Б | Номер блока копии MFT.Используется в том случае, когда первые 5 записей MFT (файл USERS, GROUPS, ASFS, MFT, Корневой каталог) повреждены. Данный блок находится в середине блоков данных. |
| CRC | uint | 4Б | Контроль данных. Используется для проверки целостности данных в файле MFT. |

Файл MFT – файл, который содержит в себе информацию о всех файлах и каталогах, которые находятся в системе.

Структура файла MFT представлена в таблице ниже.

| Название поля | Тип | Размерность | Описание |
| ------------- | --- | ----------- | -------- |
| Record_Number | uint | 4Б | Номер записи. Это уникальный номер записи, который используется для создания дерева каталогов. |
| Number_Allocated_Blocks | uint | 4Б | Количество выделенных блоков под файл. |
| FileName | char[25] | 50Б | Имя файла. |
| BusyByte | ulong | 8Б | Количество занятых байтов. |
| FileType | char | 2Б | Тип файла (″-″ - файл, ″d″ - директория). |
| Base_Record_Number | uint | 4Б | Номер базовой записи или номер родителя файла.Используется для расположения фалов по каталогам. |
| Access_Level | char[8] | 16Б | Уровень доступа к файлу. Определяет доступ к файлу на чтение, запись или выполнение. |
| UserID | uint | 4Б | Идентификационный номер пользователя, который является создателем (владельцем) файла. |
| File_Attributes | uint | 4Б | Атрибуты файла (зарезервирован, системный, скрыт). |
| Time | long | 8Б | Время последней модификации файла. |
| AdressBlockFile | uint | 4Б | Первый или начальный номер блока данных, в котором хранятся данные и используется для дальнейшей навигации в системе. |

Первые 8 записей файле MFT:
1.	MFT – таблица каталогов и файлов
2.	ASFS – список свободных/занятых кластеров
3.	GROUP – таблица групп пользователей
4.	USERS – таблица пользователей системы
5.	. – корневой каталог
6.	USERS – каталог, который содержит домашние папки пользователей системы
7.	SYSTEM – каталог, который содержит папки удалённых пользователей системы. 
8.	Папка пользователя

#### Список свободных/занятых кластеров

Каждая запись представлена числом – адресом на следующий блок. Значения могут быть адресом следующего блока и зарезервированным значением.
Максимальное число блоков = 4294967296 – 3 = 4294967293 блока.
Тип данных – uint.
Данный список представлен в виде таблицы (прототип файловой системы FAT), которая используется для навигации в ФС.

Зарезервированные значения представлены в таблице ниже

| Значение | Описание |
| -------- | -------- |
| 4294967295 | Битый сектор |
| 4294967294 | Конец файла |
| 4294967293 | Свободный блок |

#### Файл GROUPS

Файл GROUPS – файл, который хранит данные о группах, находящихся в системе. 
Данный файл представляет собой набор записей одной структуры, представленной в таблице ниже.
При форматировании системы создаются две группы пользователей:
1.	Administrator – группа, в которой состоит администратор и которая имеет высший приоритет.
2.	User – группа пользователей.

| Название поля | Тип | Размерность | Описание |
| ------------- | --- | ----------- | -------- |
| IDGroup | uint | 4Б | Уровень доступа к файлу. Определяет доступ к файлу на чтение, запись или выполнение. |
| NameGroup | uint | 40Б | Название группы. |
| AccessLevel | uint | 4Б | Уровень доступа группы. |

#### Файл USERS

Файл USERS – хранит всех пользователей в системе. Представляет
собой набор записей, каждая из которых описывает информацию о каждом отдельном пользователе. 

Структура файла USERS представлена в таблице ниже.

| Название поля | Тип | Размерность | Описание |
| ------------- | --- | ----------- | -------- |
| Id_User | uint | 4Б | Идентификационный номер пользователя. У администратора это поле по умолчанию всегда будет равным 1. |
| User_Name | char[50] | 100Б | Имя пользователя системы. |
| Login | char[15] | 30Б | Логин пользователя. Используется для авторизации пользователя в системе. |
| Hesh_Password | char[20] | 40Б | Хеш сумма пароля. Зашифрованное слово, которое пользователь использует для авторизации в системе. |
| Home_Directory | char[28] | 48Б | Домашняя директория пользователя. Директория, в которой пользователь находиться после авторизации и в которой не может никто зайти с уровнем доступа группы меньше его. |
| Id_Group | uint | 4Б | Уникальный номер группы, в которой состоит данный пользователь. |

#### Блоки данных

Блоки данных представляют собой логически разбитое место на жестком диске размером, согласно размеру кластера. Блоки данных не имеют какой-либо структуры в связи с тем, что там хранятся данные файла.

#### Копия MFT

Копия MFT – копия первого блока в середине жесткого диска. Она предназначена для резервного копирования.  В случае повреждения сектора с блоком MFT будет считываться резервная копия MFT.

#### Виртуальные страницы

Виртуальная память - совмещение оперативной памяти и временного хранилища файлов на жестком диске или винчестере. В случае если памяти ОЗУ не совсем достаточно, данные из оперативной памяти перемещаются во временное хранилище, которое называется виртуальной страницей. 
Для выполняющейся программы данный метод не требует дополнительных усилий со стороны программиста, однако реализация этого метода требует аппаратной поддержки, а также поддержки со стороны операционной системы. 
Данная технология разработана для многозадачных операционных систем. Он позволяет увеличить эффективность использования памяти несколькими одновременно работающими программами, организовав множество независимых адресных пространств, и обеспечить защиту памяти между различными приложениями.
При использовании виртуальной памяти упрощается разработка программ, благодаря вышеназванному механизму.
Применение виртуальной памяти позволяет: 
•	Освободить разработчика от необходимости вручную управлять загрузкой частей программы в память и согласовывать использование памяти с другими программами;
•	Предоставление программам больше памяти, чем физически установлено в системе;
•	Каждому приложению назначается своё адресное пространство, что изолирует выполняющиеся программы друг от друга. 
•	Повысить безопасность за счёт защиты памяти.

В настоящее время эта технология имеет аппаратную поддержку на всех современных процессорах. В то же время во встраиваемых системах и в системах специального назначения, где требуется очень быстрая работа или есть ограничения на длительность отклика, виртуальная память используется относительно редко.
В большинстве современных операционных систем виртуальная память организуется с помощью страничной адресации.
В рамках курсового проекта должна быть описаны динамические разделы виртуальной памяти.
При распределении памяти динамическими разделами память машины не делится заранее на разделы. С самого начала вся память, отводимая для приложений, свободна. Каждому вновь поступающему на выполнение приложению на этапе создания процесса выделяется вся необходимая ему память (если достаточный объем памяти отсутствует, то приложение не принимается на выполнение и процесс для него не создается). После завершения процесса память освобождается, и на это место может быть загружен другой процесс. 
Таким образом, в произвольный момент времени оперативная память представляет собой случайную последовательность занятых и свободных участков (разделов) произвольного размера.
На рисунке ниже показано состояние памяти в различные моменты времени при использовании динамического распределения.
[![](https://github.com/SemikAlexander/FAT/blob/master/Images/memory.png)](https://github.com/SemikAlexander/FAT/blob/master/Images/memory.png "Состояние памяти в различные моменты времени при использовании динамического распределения")
