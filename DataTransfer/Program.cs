using System.Xml;
using System.Xml.Linq;
using MySql.Data.MySqlClient;

Settings settings = new Settings();
string[] tableNames = new string[3] { "dt.fileforanalysis", "dt.devicestatus", "dt.usersdata" };

LoadingDevicesData loadingDevices = new LoadingDevicesData();
UploadingSettings uploadingSettings = new UploadingSettings();
LoadingDevicesHistory loadingDevicesHistory = new LoadingDevicesHistory();
UploadingTheDatabase uploadingTheDatabase = new UploadingTheDatabase();

settings = uploadingSettings.uploadingSettings(settings); // первая запись
settings = loadingDevices.loadingDevicesData(settings); // перезапись
//settings = loadingDevicesHistory.loadingDevicesHistory(settings); // добовление данных истории устройств

uploadingTheDatabase.uploadingTheDatabase(settings);

Console.WriteLine("Программа завершила работу");
Console.ReadLine();

//foreach (var devices in settings.devicesDatas)
//{
//    Console.WriteLine($"devices:{devices.deviceID} {devices.userID} {devices.sdatetimeSTR} {devices.edatetimeSTR};");
//}


//foreach (string itm in settings.fileForAnalysis)
//{
//    Console.Write($"fileForAnalysis: '{itm}'; ");
//}
//Console.WriteLine();
//foreach (var itm in settings.deviceStatuses)
//{
//    Console.Write($" deviceStatuses: {itm.DeviceID},{itm.DeviceDescription}, {itm.Comment};");
//}
//Console.WriteLine();
//foreach (var itm in settings.usersDatas)
//{
//    Console.Write($" usersDatas: {itm.UserID},{itm.UserNames}, {itm.UserDepartment};");
//}

//Console.WriteLine($"dTSettingsSQL: {settings.dTSettingsSQL.port} {settings.dTSettingsSQL.name} {settings.dTSettingsSQL.password} {settings.dTSettingsSQL.hostname}");
//Console.WriteLine($"{settings.pathFiles[0]}");
//Console.WriteLine($"{settings.pathFiles[1]}");
//Console.WriteLine($"{settings.pathFiles[2]}");


Console.ReadLine();

public class UploadingTheDatabase
{
    public void ConnectDB(Settings settings, string query)
    {
        MySqlConnection mySqlConnection = new MySqlConnection(
            $"server={settings.dTSettingsSQL.hostname};" +
            $"database={settings.dTSettingsSQL.dataBase};" +
            $"user={settings.dTSettingsSQL.name};" +
            $"password={settings.dTSettingsSQL.password};"
            );

        MySqlCommand mySqlCommand;

        try
        {
            mySqlConnection.Open();
            Console.WriteLine("Соединение с бд установленно");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            mySqlCommand = new MySqlCommand(query, mySqlConnection);

            string str;

            if (mySqlCommand.ExecuteScalar() != null)
            {
                str = mySqlCommand.ExecuteScalar().ToString();
                Console.WriteLine(str);
            }

            mySqlConnection.Close();
            Console.WriteLine("Соединение с бд закрыто");
        }
    }
    public void uploadingTheDatabase(Settings settings)
    {
        string sqlQueryInsert = "insert into dt.fileforanalysis (NameOfDevice) value ('T4')";

        try
        {
            // удаляем старые данные
            ConnectDB(settings, $"DROP TABLE IF EXISTS {settings.dTSettingsSQL.tablefileforanalysis}");
            ConnectDB(settings, $"DROP TABLE IF EXISTS {settings.dTSettingsSQL.tableusersdata}");
            ConnectDB(settings, $"DROP TABLE IF EXISTS {settings.dTSettingsSQL.tabledevicestatus}");

            foreach (string devices in settings.fileForAnalysis)
            {
                ConnectDB(settings, $"DROP TABLE IF EXISTS {devices}");
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            QuerysSQLGenTable querysSQLGenTable = new QuerysSQLGenTable(settings);
            ConnectDB(settings, querysSQLGenTable.devicestatus);
            ConnectDB(settings, querysSQLGenTable.fileforanalysis);
            ConnectDB(settings, querysSQLGenTable.usersdata);

            foreach (string devices in settings.fileForAnalysis)
            {
                ConnectDB(settings, new QuerysSQLGenTable(settings, devices).templateCreatingTableDevices);
            }
        }

    }
}
public struct QuerysSQLGenTable
{
    public string devicestatus;
    public string fileforanalysis;
    public string usersdata;
    public string templateCreatingTableDevices;

    public QuerysSQLGenTable(Settings settings, string tableName = "T0000000")
    {
        devicestatus = $"CREATE TABLE {settings.dTSettingsSQL.tabledevicestatus} " +
        "(`idDeviceStatus` int NOT NULL AUTO_INCREMENT," +
        "`DeviceID` varchar(45) NOT NULL," +
        "`DeviceDescription` varchar(45) NOT NULL," +
        "`Comment` varchar(45) NOT NULL COMMENT 'Файл о состоянии устройств'," +
        $"PRIMARY KEY (idDeviceStatus)" +
        ") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;";
        fileforanalysis = $"CREATE TABLE {settings.dTSettingsSQL.tablefileforanalysis} " +
        "(`idFileForAnalysis` int NOT NULL AUTO_INCREMENT," +
        "`NameOfDevice` varchar(45) NOT NULL COMMENT 'Файл для анализа устройств'," +
        $"PRIMARY KEY (idFileForAnalysis)" +
        ") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;";
        usersdata = $"CREATE TABLE {settings.dTSettingsSQL.tableusersdata} " +
        "(`idUsersData` int NOT NULL AUTO_INCREMENT," +
        "`UserID` varchar(45) NOT NULL," +
        "`UserNames` varchar(45) NOT NULL," +
        "`UserDepartment` varchar(45) NOT NULL," +
        $"PRIMARY KEY (idUsersData)" +
        ") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;";
        templateCreatingTableDevices = $"DROP TABLE IF EXISTS {tableName};CREATE TABLE {tableName} " +
        $"(`{tableName}` INT NOT NULL AUTO_INCREMENT," +
        "`deviceID` VARCHAR(45) NOT NULL," +
        "`userID` VARCHAR(45) NOT NULL," +
        "`sdatetimeSTR` VARCHAR(45) NULL," +
        "`edatetimeSTR` VARCHAR(45) NULL," +
        $"PRIMARY KEY (`{tableName}`)" +
        ") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;";
    }
}
public class LoadingDevicesHistory
{
    public Settings loadingDevicesHistory(Settings settings)
    {

        foreach (string device in settings.fileForAnalysis)//phones powerbanks tablets
        {
            if (device.ToCharArray()[0] == 'T' && device.ToCharArray()[1] != 'A')
            {
                string d1 = "", d2 = "", d3 = "", d4 = "";
                XmlDocument document = new XmlDocument();
                document.Load($"{settings.pathFilesDevice[0]}{device}.xml");

                XmlElement xmlElement = document.DocumentElement;

                foreach (XmlNode item in xmlElement)
                {
                    foreach (XmlNode item1 in item)
                    {
                        if (item1.Name == "deviceID")
                            d1 = item1.InnerText;

                        if (item1.Name == "userID")
                            d2 = item1.InnerText;

                        if (item1.Name == "sdatetimeSTR")
                            d3 = item1.InnerText;

                        if (item1.Name == "edatetimeSTR")
                        {
                            d4 = item1.InnerText;
                            settings.devicesDatas.Add(new Devices() { deviceID = d1 == "" ? device : d1, userID = d2, sdatetimeSTR = d3, edatetimeSTR = d4 });
                            d1 = ""; d2 = ""; d3 = ""; d4 = "";
                        }

                    }
                }
            }
            if (device.ToCharArray()[0] == 'T' && device.ToCharArray()[1] == 'A' && device.ToCharArray()[2] == 'B')
            {
                string d1 = "", d2 = "", d3 = "", d4 = "";
                XmlDocument document = new XmlDocument();
                document.Load($"{settings.pathFilesDevice[2]}{device}.xml");

                XmlElement xmlElement = document.DocumentElement;

                foreach (XmlNode item in xmlElement)
                {
                    foreach (XmlNode item1 in item)
                    {
                        if (item1.Name == "deviceID")
                            d1 = item1.InnerText;

                        if (item1.Name == "userID")
                            d2 = item1.InnerText;

                        if (item1.Name == "sdatetimeSTR")
                            d3 = item1.InnerText;

                        if (item1.Name == "edatetimeSTR")
                        {
                            d4 = item1.InnerText;
                            settings.devicesDatas.Add(new Devices() { deviceID = d1 == "" ? device : d1, userID = d2, sdatetimeSTR = d3, edatetimeSTR = d4 });
                            d1 = ""; d2 = ""; d3 = ""; d4 = "";
                        }

                    }
                }
            }
            if (device.ToCharArray()[0] == 'P')
            {
                string d1 = "", d2 = "", d3 = "", d4 = "";
                XmlDocument document = new XmlDocument();
                document.Load($"{settings.pathFilesDevice[1]}{device}.xml");

                XmlElement xmlElement = document.DocumentElement;

                foreach (XmlNode item in xmlElement)
                {
                    foreach (XmlNode item1 in item)
                    {
                        if (item1.Name == "deviceID")
                            d1 = item1.InnerText;

                        if (item1.Name == "userID")
                            d2 = item1.InnerText;

                        if (item1.Name == "sdatetimeSTR")
                            d3 = item1.InnerText;

                        if (item1.Name == "edatetimeSTR")
                        {
                            d4 = item1.InnerText;
                            settings.devicesDatas.Add(new Devices() { deviceID = d1 == "" ? device : d1, userID = d2, sdatetimeSTR = d3, edatetimeSTR = d4 });
                            d1 = ""; d2 = ""; d3 = ""; d4 = "";
                        }

                    }
                }
            }
        }

        return settings;
    }
}
public class LoadingDevicesData
{
    public Settings loadingDevicesData(Settings settings)
    {
        for (int i = 0; i < 3; i++)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(settings.pathFiles[i]); //fileforanalysis devicestatus usersdata
            string d1 = "", d2 = "", d3 = "";
            string d4 = "", d5 = "", d6 = "";
            XmlElement xmlElement = xmlDocument.DocumentElement;

            switch (i)
            {
                case 0:
                    foreach (XmlNode item in xmlElement)
                        settings.fileForAnalysis.Add(item.InnerText);
                    break;

                case 1:
                    foreach (XmlNode item in xmlElement)
                    {
                        foreach (XmlNode item1 in item)
                        {
                            if (item1.Name == "DeviceID")
                                d1 = item1.InnerText;
                            if (item1.Name == "DeviceDescription")
                                d2 = item1.InnerText;
                            if (item1.Name == "Comment")
                            {
                                d3 = item1.InnerText;
                                settings.deviceStatuses.Add(new DeviceStatus() { DeviceID = d1, DeviceDescription = d2, Comment = d3 });
                                d1 = ""; d2 = ""; d3 = "";
                            }

                        }
                    }
                    break;

                case 2:
                    foreach (XmlNode item in xmlElement)
                    {
                        foreach (XmlNode item1 in item)
                        {
                            if (item1.Name == "UserID")
                                d4 = item1.InnerText;
                            if (item1.Name == "UserNames")
                                d5 = item1.InnerText;
                            if (item1.Name == "UserDepartment")
                            {
                                d6 = item1.InnerText;
                                settings.usersDatas.Add(new UsersData() { UserID = d4, UserNames = d5, UserDepartment = d6 });
                                d4 = ""; d5 = ""; d6 = "";
                            }

                        }
                    }
                    break;

            }
        }

        return settings;
    }
}
public class UploadingSettings
{
    public Settings uploadingSettings(Settings settings)
    {
        XmlDocument SettingsFile = new XmlDocument();
        SettingsFile.Load("Setting.xml");

        XmlElement xmlElementSettings = SettingsFile.DocumentElement;

        if (!xmlElementSettings.IsEmpty)
        {
            foreach (XmlNode elem in xmlElementSettings)
            {
                foreach (XmlNode item in elem)
                {
                    if (item.Name == "port")
                        settings.dTSettingsSQL.port = Convert.ToInt32(item.InnerText);
                    if (item.Name == "name")
                        settings.dTSettingsSQL.name = item.InnerText;
                    if (item.Name == "password")
                        settings.dTSettingsSQL.password = Convert.ToInt32(item.InnerText);
                    if (item.Name == "hostname")
                        settings.dTSettingsSQL.hostname = item.InnerText;

                    if (item.Name == "fileforanalysis")
                        settings.pathFiles[0] = item.InnerText;

                    if (item.Name == "devicestatus")
                        settings.pathFiles[1] = item.InnerText;

                    if (item.Name == "usersdata")
                        settings.pathFiles[2] = item.InnerText;

                    if (item.Name == "phones")
                        settings.pathFilesDevice[0] = item.InnerText;

                    if (item.Name == "powerbanks")
                        settings.pathFilesDevice[1] = item.InnerText;

                    if (item.Name == "tablets")
                        settings.pathFilesDevice[2] = item.InnerText;

                    if (item.Name == "database")
                        settings.dTSettingsSQL.dataBase = item.InnerText;

                    if (item.Name == "dataBasefileforanalysis")
                        settings.dTSettingsSQL.tablefileforanalysis = item.InnerText;

                    if (item.Name == "dataBasedevicestatus")
                        settings.dTSettingsSQL.tabledevicestatus = item.InnerText;

                    if (item.Name == "dataBaseusersdata")
                        settings.dTSettingsSQL.tableusersdata = item.InnerText;
                }
            }

            return settings;
        }
        else
        {
            Console.WriteLine("File is empty");

            return new Settings();
        }
    }
}
public class Settings
{
    public string[] pathFiles = new string[3];
    public string[] pathFilesDevice = new string[3]; //phones powerbanks tablets
    public DTSettingsSQL dTSettingsSQL = new DTSettingsSQL();

    public List<string> fileForAnalysis = new List<string>();
    public List<DeviceStatus> deviceStatuses = new List<DeviceStatus>();
    public List<UsersData> usersDatas = new List<UsersData>();

    public List<Devices> devicesDatas = new List<Devices>();
}
public struct Devices
{
    public string deviceID;
    public string userID;
    public string sdatetimeSTR;
    public string edatetimeSTR;
}
public struct DeviceStatus
{
    public string DeviceID;
    public string DeviceDescription;
    public string Comment;
}
public struct UsersData
{
    public string UserID;
    public string UserNames;
    public string UserDepartment;
}
public class DTSettingsSQL
{
    public int port;
    public string name;
    public int password;
    public string hostname;
    public string dataBase;
    public string tabledevicestatus;
    public string tableusersdata;
    public string tablefileforanalysis;
}