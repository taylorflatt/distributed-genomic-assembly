namespace Genome.Helpers
{
    /// <summary>
    /// Contains a list of public locations for different items that are used throughout the program.
    /// </summary>
    public class Accessors
    {
        //protected internal static string masterPath
        //{
        //    get
        //    {
        //        return masterPath;
        //    }

        //    set
        //    {
        //        masterPath = "/share/scratch/bioinfo/" + HelperMethods.GetUsername();
        //    }
        //}

        /// <summary>
        /// List of constants. Here, BigDog is referred to as BD for brevity.
        /// </summary>
        protected internal static string USER_ROOT_JOB_DIRECTORY = "/share/scratch/bioinfo/" + HelperMethods.GetUsername();
        protected internal const string WEBSITE_IP = "131.230.63.114";
        protected internal const int BD_PORT = 22;
        protected internal const string PUBLIC_KEY_PATH = "UNKNOWN";
        protected internal const string PRIVATE_KEY_PATH = @"C:\Connection Information\private-key-openssh-tf.ppk";
        protected internal const string UPDATE_ACCT = "tflatt"; // BigDog acct we use to update jobs.
        protected internal const string BD_IP = "login-0-0.research.siu.edu";
        protected internal const string BD_UPDATE_KEY_PATH = "FILE SERVER LOCATION OF BIG DOG PRIVATE KEY LOCATION";
        protected internal const string BD_COMPUTE_NODE1 = "compute-0-24";
        protected internal const string BD_COMPUTE_NODE2 = "compute-0-25";
        protected internal const string ZIP_STORAGE_PATH = "LOCATION ON THE FTP SERVER";
        protected internal const string FTP_URL = "ftp://" + WEBSITE_IP;
        protected internal const string VERIFY_PERMISSIONS_TEST_DIR = "/share/scratch/bioinfo/testPermissions";
        protected internal const int MINIMUM_QUOTA = 500;

        /// <summary>
        /// Gets the name of the job (or relative path to the job folder). 
        /// </summary>
        /// <param name="seed">The unique identifier for a particular job.</param>
        /// <returns>Returns a string directory name for the job.</returns>
        /// <remarks>Used primarily to pass the relative name of the job directory into the ZIP function.</remarks>
        protected internal static string GetRelativeJobDirectory(int seed)
        {
            return "Job" + seed;
        }

        /// <summary>
        /// Generates the FTP path (url) to the init script.
        /// </summary>
        /// <param name="seed">The unique identifier for a particular job.</param>
        /// <param name="username">The username of the person submitting the job.</param>
        /// <returns>A string url to the init script.</returns>
        protected internal static string GetInitScriptPath(int seed, string username)
        {
            return FTP_URL + "AssemblerConfigs/" + "Job-" + username + seed + "/" + "init_" + seed + ".sh";
        }

        /// <summary>
        /// Generates the FTP path (url) to the masurca script.
        /// </summary>
        /// <param name="seed">The unique identifier for a particular job.</param>
        /// <param name="username">The username of the person submitting the job.</param>
        /// <returns>A string url to the masurca script.</returns>
        protected internal static string GetMasurcaScriptPath(int seed, string username)
        {
            return FTP_URL + "AssemblerConfigs/" + "Job-" + username + seed + "/" + "MasurcaConfig_" + seed + ".txt";
        }

        /// <summary>
        /// Get the link for the data download for the user.
        /// </summary>
        /// <param name="username">Username of the user (without the @).</param>
        /// <param name="seed">The unique identifier for a particular job.</param>
        /// <param name="zipName">The name of the zip.</param>
        /// <returns>The path to the download link on the FTP server.</returns>
        protected internal static string GetDataDownloadLink(int seed, string username, string zipName)
        {
            return FTP_URL + "AssemblerJobs/" + "Job-" + username + seed + "/" + zipName;
        }

        /// <summary>
        /// Grabs the working directory of the entire job.
        /// </summary>
        /// <param name="seed">The unique identifier for a particular job.</param>
        /// <returns>Returns a string path to the working directory.</returns>
        protected internal static string GetJobPath(int seed)
        {
            return USER_ROOT_JOB_DIRECTORY + "/Job"  + seed + "/";
        }

        /// <summary>
        /// Grabs the data directory of the job.
        /// </summary>
        /// <param name="seed">The unique identifier for a particular job.</param>
        /// <returns>Returns a string path to the data directory.</returns>
        protected internal static string GetJobDataPath(int seed)
        {
            return GetJobPath(seed) + "Data/";
        }

        /// <summary>
        /// Grabs the config directory of the job.
        /// </summary>
        /// <param name="seed">The unique identifier for a particular job.</param>
        /// <returns>Returns a string path to the config directory.</returns>
        protected internal static string GetJobConfigPath(int seed)
        {
            return GetJobPath(seed) + "Config/";
        }

        /// <summary>
        /// Grabs the output directory of the job.
        /// </summary>
        /// <param name="seed">The unique identifier for a particular job.</param>
        /// <returns>Returns a string path to the output directory.</returns>
        protected internal static string GetJobOutputPath(int seed)
        {
            return GetJobPath(seed) + "Output/";
        }

        /// <summary>
        /// Grabs the log directory of the job.
        /// </summary>
        /// <param name="seed">The unique identifier for a particular job.</param>
        /// <returns>Returns a string path to the log directory.</returns>
        protected internal static string GetJobLogPath(int seed)
        {
            return GetJobPath(seed) + "Log/";
        }

        /// <summary>
        /// Grabs the masurca output directory of the job.
        /// </summary>
        /// <param name="seed">The unique identifier for a particular job.</param>
        /// <returns>Returns a string path to the masurca output directory.</returns>
        protected internal static string GetMasurcaOutputPath(int seed)
        {
            return GetJobOutputPath(seed) + "/Masurca/";
        }

        /// <summary>
        /// Grabs the sga output directory of the job.
        /// </summary>
        /// <param name="seed">The unique identifier for a particular job.</param>
        /// <returns>Returns a string path to the sga output directory.</returns>
        protected internal static string GetSgaOutputPath(int seed)
        {
            return GetJobOutputPath(seed) + "/SGA/";
        }

        /// <summary>
        /// Grabs the wgs output directory of the job.
        /// </summary>
        /// <param name="seed">The unique identifier for a particular job.</param>
        /// <returns>Returns a string path to the wgs output directory.</returns>
        protected internal static string GetWgsOutputPath(int seed)
        {
            return GetJobOutputPath(seed) + "/WGS/";
        }

        /// <summary>
        /// Gets the location of the compressed data on BD.
        /// </summary>
        /// <param name="seed">The unique identifier for a particular job.</param>
        /// <returns>Returns a string path to the zip file.</returns>
        protected internal static string GetCompressedDataPath(int seed)
        {
            return GetJobOutputPath(seed) + "job-" + seed + ".zip";
        }

        /// <summary>
        /// Gets the success log path for masurca. Default log is the output log. Set errorLog to true if the error log is desired instead.
        /// </summary>
        /// <param name="seed">The unique identifier for a particular job.</param>
        /// <param name="errorLog">Flag as to whether or not you want the error log instead of the output log.</param>
        /// <returns>Returns a string path to the success log.</returns>
        protected internal static string GetMasurcaSuccessLogPath(int seed, bool errorLog = false)
        {
            return errorLog ? GetMasurcaOutputPath(seed) + "masurca_success.elog" : GetMasurcaOutputPath(seed) + "masurca_success.olog";
        }

        /// <summary>
        /// Gets the failure log path for masurca. Default log is the output log. Set errorLog to true if the error log is desired instead.
        /// </summary>
        /// <param name="seed">The unique identifier for a particular job.</param>
        /// <param name="errorLog">Flag as to whether or not you want the error log instead of the output log.</param>
        /// <returns>Returns a string path to the failure log.</returns>
        protected internal static string GetMasurcaFailureLogPath(int seed, bool errorLog = false)
        {
            return errorLog ? GetMasurcaOutputPath(seed) + "masurca_failure.elog" : GetMasurcaOutputPath(seed) + "masurca_failure.olog";
        }
    }
}