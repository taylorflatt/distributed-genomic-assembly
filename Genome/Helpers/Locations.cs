namespace Genome.Helpers
{
    public class Locations
    {
        public static string GetJobPath(int id)
        {
            return "/share/scratch/tflatt/Job" + id + "/";
        }

        public static string GetJobDataPath(int id)
        {
            return GetJobPath(id) + "Data/";
        }

        public static string GetJobConfigPath(int id)
        {
            return GetJobPath(id) + "Config/";
        }

        public static string GetJobOutputPath(int id)
        {
            return GetJobPath(id) + "Output/";
        }

        public static string GetJobLogPath(int id)
        {
            return GetJobPath(id) + "Log/";
        }

        public static string GetMasurcaOutputPath(int id)
        {
            return GetJobOutputPath(id) + "/Masurca/";
        }

        public static string GetSgaOutputPath(int id)
        {
            return GetJobOutputPath(id) + "/SGA/";
        }

        public static string GetWgsOutputPath(int id)
        {
            return GetJobOutputPath(id) + "/WGS/";
        }

        public static string GetInitScriptPath()
        {
            return "LOCATION ON THE FTP SERVER";
        }

        public static string GetMasurcaScriptPath()
        {
            return "LOCATION ON THE FTP SERVER";
        }

        public static string GetFtpUrl()
        {
            return "URL TO THE FTP";
        }

        public static string GetCompressedDataPath(int id)
        {
            return GetJobOutputPath(id) + "job" + id + ".zip";
        }
    }
}