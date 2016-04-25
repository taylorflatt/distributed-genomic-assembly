namespace Genome.Helpers
{
    public class Locations
    {
        public static string GetMasterPath()
        {
            return "/share/scratch/tflatt/";
        }

        public static string GetJobPath(int id)
        {
            return GetMasterPath() + "/Job" + id + "/";
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

        public static string GetZipFileStoragePath()
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

        public static string GetMasurcaErrorSuccessLogPath(int id)
        {
            return GetJobLogPath(id) + "masurca_success.elog";
        }

        public static string GetMasurcaOutputSuccessLogPath(int id)
        {
            return GetJobLogPath(id) + "masurca_success.olog";
        }

        public static string GetMasurcaErrorFailureLogPath(int id)
        {
            return GetJobLogPath(id) + "masurca_failure.elog";
        }

        public static string GetMasurcaOutputFailureLogPath(int id)
        {
            return GetJobLogPath(id) + "masurca_failure.olog";
        }

        public static string GetSgaErrorSuccessLogPath(int id)
        {
            return GetJobLogPath(id) + "sga_success.elog";
        }

        public static string GetSgaOutputSuccessLogPath(int id)
        {
            return GetJobLogPath(id) + "sga_success.olog";
        }

        public static string GetSgaErrorFailureLogPath(int id)
        {
            return GetJobLogPath(id) + "sga_failure.elog";
        }

        public static string GetSgaOutputLogPath(int id)
        {
            return GetJobLogPath(id) + "sga_failure.olog";
        }

        public static string GetWgsErrorSuccessLogPath(int id)
        {
            return GetJobLogPath(id) + "wgs_success.elog";
        }

        public static string GetWgsOutputSuccessLogPath(int id)
        {
            return GetJobLogPath(id) + "wgs_success.olog";
        }

        public static string GetWgsErrorFailureLogPath(int id)
        {
            return GetJobLogPath(id) + "wgs_failure.elog";
        }

        public static string GetWgsOutputFailureLogPath(int id)
        {
            return GetJobLogPath(id) + "wgs_failure.olog";
        }
    }
}