namespace AuthProjWebApi.Packages
{
    public class PKG_BASE
    {
        string connStr;

        public PKG_BASE()
        {
            connStr = @"Data Source=(DESCRIPTION =  (ADDRESS = (PROTOCOL = TCP)(HOST = 172.20.0.188)(PORT = 1521)) 
                                                                    (CONNECT_DATA =   (SERVER = DEDICATED) (SID = orcl)));
                                                                    User Id=olerning;Password=olerning";
        }

        protected string ConnStr
        {
            get { return connStr; }
        }


    }
}
