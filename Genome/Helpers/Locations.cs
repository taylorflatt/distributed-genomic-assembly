namespace Genome.Helpers
{
    /// <summary>
    /// Contains a list of public locations for different items that are used throughout the program.
    /// </summary>
    public class Locations
    {
        /// <summary>
        /// List of constants. Here, BigDog is referred to as BD for brevity.
        /// </summary>
        protected internal const string WEBSITE_IP = "131.230.63.114";
        protected internal const string PUBLIC_KEY_PATH = "UNKNOWN";
        protected internal const string GET_MASTER_PATH = "/share/scratch/bioinfo/";
        protected internal const string BD_IP = "login-0-0.research.siu.edu";
        protected internal const string BD_UPDATE_KEY_PATH = "FILE SERVER LOCATION OF BIG DOG PRIVATE KEY LOCATION";
        protected internal const string BD_COMPUTE_NODE1 = "compute-0-24";
        protected internal const string BD_COMPUTE_NODE2 = "compute-0-25";
        protected internal const string ZIP_STORAGE_PATH = "LOCATION ON THE FTP SERVER";
        protected internal const string FTP_URL = "ftp://" + WEBSITE_IP;

        protected internal static string GetInitScriptPath(int seed, string username)
        {
            return FTP_URL + "AssemblerConfigs/" + "Job-" + username + seed + "/" + "init_" + seed + ".sh";
        }

        protected internal static string GetMasurcaScriptPath(int seed, string username)
        {
            return FTP_URL + "AssemblerConfigs/" + "Job-" + username + seed + "/" + "MasurcaConfig_" + seed + ".txt";
        }

        //protected internal static string GetUrlTestDirectory(int seed)
        //{
        //    return "/share/scratch/bioinfo/GENOME_ASSEMBLER_test_urls_" + seed;
        //}

        /// <summary>
        /// Get the link for the data download for the user.
        /// </summary>
        /// <param name="username">Username of the user (without the @).</param>
        /// <param name="uuid">The unique ID for the job.</param>
        /// <returns>The path to the download link on the FTP server.</returns>
        protected internal static string GetDataDownloadLink(string username, int uuid)
        {
            return "ROUTE TO THE FTP ON THE FILE SERVER/" + username + "Job" + uuid;
        }

        /// <summary>
        /// Grabs the working directory of the entire job.
        /// </summary>
        /// <param name="id">The unique ID for the job.</param>
        /// <returns>Returns a string path to the working directory.</returns>
        protected internal static string GetJobPath(int id)
        {
            return GET_MASTER_PATH + "/Job" + id + "/";
        }

        /// <summary>
        /// Grabs the data directory of the job.
        /// </summary>
        /// <param name="id">The unique ID for the job.</param>
        /// <returns>Returns a string path to the data directory.</returns>
        protected internal static string GetJobDataPath(int id)
        {
            return GetJobPath(id) + "Data/";
        }

        /// <summary>
        /// Grabs the config directory of the job.
        /// </summary>
        /// <param name="id">The unique ID for the job.</param>
        /// <returns>Returns a string path to the config directory.</returns>
        protected internal static string GetJobConfigPath(int id)
        {
            return GetJobPath(id) + "Config/";
        }

        /// <summary>
        /// Grabs the output directory of the job.
        /// </summary>
        /// <param name="id">The unique ID for the job.</param>
        /// <returns>Returns a string path to the output directory.</returns>
        protected internal static string GetJobOutputPath(int id)
        {
            return GetJobPath(id) + "Output/";
        }

        /// <summary>
        /// Grabs the log directory of the job.
        /// </summary>
        /// <param name="id">The unique ID for the job.</param>
        /// <returns>Returns a string path to the log directory.</returns>
        protected internal static string GetJobLogPath(int id)
        {
            return GetJobPath(id) + "Log/";
        }

        /// <summary>
        /// Grabs the masurca output directory of the job.
        /// </summary>
        /// <param name="id">The unique ID for the job.</param>
        /// <returns>Returns a string path to the masurca output directory.</returns>
        protected internal static string GetMasurcaOutputPath(int id)
        {
            return GetJobOutputPath(id) + "/Masurca/";
        }

        /// <summary>
        /// Grabs the sga output directory of the job.
        /// </summary>
        /// <param name="id">The unique ID for the job.</param>
        /// <returns>Returns a string path to the sga output directory.</returns>
        protected internal static string GetSgaOutputPath(int id)
        {
            return GetJobOutputPath(id) + "/SGA/";
        }

        /// <summary>
        /// Grabs the wgs output directory of the job.
        /// </summary>
        /// <param name="id">The unique ID for the job.</param>
        /// <returns>Returns a string path to the wgs output directory.</returns>
        protected internal static string GetWgsOutputPath(int id)
        {
            return GetJobOutputPath(id) + "/WGS/";
        }

        /// <summary>
        /// Gets the location of the compressed data on BD.
        /// </summary>
        /// <param name="id">The unique ID for the job.</param>
        /// <returns>Returns a string path to the zip file.</returns>
        protected internal static string GetCompressedDataPath(int id)
        {
            return GetJobOutputPath(id) + "job" + id + ".zip";
        }

        /// <summary>
        /// Gets the success log path for masurca. Default log is the output log. Set errorLog to true if the error log is desired instead.
        /// </summary>
        /// <param name="id">The unique ID for the job.</param>
        /// <param name="errorLog">Flag as to whether or not you want the error log instead of the output log.</param>
        /// <returns>Returns a string path to the success log.</returns>
        protected internal static string GetMasurcaSuccessLogPath(int id, bool errorLog = false)
        {
            return errorLog ? GetMasurcaOutputPath(id) + "masurca_success.elog" : GetMasurcaOutputPath(id) + "masurca_success.olog";
        }

        /// <summary>
        /// Gets the failure log path for masurca. Default log is the output log. Set errorLog to true if the error log is desired instead.
        /// </summary>
        /// <param name="id">The unique ID for the job.</param>
        /// <param name="errorLog">Flag as to whether or not you want the error log instead of the output log.</param>
        /// <returns>Returns a string path to the failure log.</returns>
        protected internal static string GetMasurcaFailureLogPath(int id, bool errorLog = false)
        {
            return errorLog ? GetMasurcaOutputPath(id) + "masurca_failure.elog" : GetMasurcaOutputPath(id) + "masurca_failure.olog";
        }
    }
}