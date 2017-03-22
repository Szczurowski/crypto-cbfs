using System;
using CallbackFS;

namespace Crypto.Runner
{
    using System.IO;
    using ACCESS_MASK = System.UInt32;

    class Program
    {
        private const string driverPath = @"..\..\..\..\..\Drivers\cbfs.cab";
        private const string mRootPath = "Location";
        private const string mGuid = "713CC6CE-B3E2-4fd9-838D-E28F558F6866";

        static void Main(string[] args)
        {
            CallbackFileSystem cbfs = null;
            UInt32 Reboot = 0;
            try
            {
                CallbackFileSystem.SetRegistrationKey("2A65D30F47C829E1FF5CB13E4300B5A23798B17A3104CB490D3ADF3CFB8DD67236152C722D39FC244BA7B3832BC18E7696B68894075485F0259277087D2A4F182D7E83784D5E631C51FA9FBC319A3F98ADFE03CC81AACFBC319A3F9CF17E83F180");

                CallbackFileSystem.Install(driverPath, mGuid, Environment.SystemDirectory,                                                true,
                                                //CallbackFileSystem.CBFS_MODULE_NET_REDIRECTOR_DLL |
                                                    CallbackFileSystem.CBFS_MODULE_MOUNT_NOTIFIER_DLL,
                                                ref Reboot);

                var status = UpdateDriverStatus();
                Console.WriteLine(status);

                CallbackFileSystem.Initialize(mGuid);

                cbfs = new CallbackFileSystem
                {
                    OnCreateFile = new CbFsCreateFileEvent(CbFsCreateFile),
                    OnMount = new CbFsMountEvent(x => { })
                };
                cbfs.CreateStorage();

                // mount
                var dirinfo = new DirectoryInfo(mRootPath);
                if (!dirinfo.Exists)
                {
                    dirinfo.Create();
                }

                cbfs.MountMedia(0);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e}");
            }
            finally
            {
                if (null != cbfs)
                {
                    cbfs.UnmountMedia(true);
                    while (cbfs.GetMountingPointCount() != 0)
                    {
                        cbfs.DeleteMountingPoint(0);
                    }
                    cbfs.DeleteStorage(true);
                }

                CallbackFileSystem.Uninstall(driverPath, mGuid, Environment.SystemDirectory, ref Reboot);

                var status = UpdateDriverStatus();
                Console.WriteLine(status);
            }

            Console.WriteLine("Press any key for exit...");
            Console.ReadLine();
        }

        private static string UpdateDriverStatus()
        {
            bool Installed = false;

            int VersionHigh = 0, VersionLow = 0;

            SERVICE_STATUS status = new SERVICE_STATUS();

            CallbackFileSystem.GetModuleStatus(mGuid, CallbackFileSystem.CBFS_MODULE_DRIVER, ref Installed, ref VersionHigh, ref VersionLow, ref status);

            if (Installed)
            {
                string strStat;

                switch (status.currentState)
                {
                    case (int)CbFsDriverState.CBFS_SERVICE_CONTINUE_PENDING:
                        strStat = "continue is pending";
                        break;
                    case (int)CbFsDriverState.CBFS_SERVICE_PAUSE_PENDING:
                        strStat = "pause is pending";
                        break;
                    case (int)CbFsDriverState.CBFS_SERVICE_PAUSED:
                        strStat = "is paused";
                        break;
                    case (int)CbFsDriverState.CBFS_SERVICE_RUNNING:
                        strStat = "is running";
                        break;
                    case (int)CbFsDriverState.CBFS_SERVICE_START_PENDING:
                        strStat = "is starting";
                        break;
                    case (int)CbFsDriverState.CBFS_SERVICE_STOP_PENDING:
                        strStat = "is stopping";
                        break;
                    case (int)CbFsDriverState.CBFS_SERVICE_STOPPED:
                        strStat = "is stopped";
                        break;
                    default:
                        strStat = "in undefined state";
                        break;
                }

                return string.Format("Driver (ver {0}.{1}.{2}.{3}) installed, service {4}", VersionHigh >> 16, VersionHigh & 0xFFFF, VersionLow >> 16, VersionLow & 0xFFFF, strStat);
            }
            else
            {
                return "Driver not installed";
            }
        }

        private static void CbFsCreateFile(object sender,
            string FileName,
            ACCESS_MASK DesiredAccess,
            uint FileAttributes,
            uint ShareMode,
            CbFsFileInfo FileInfo,
            CbFsHandleInfo HandleInfo
            )
        {
        }
    }
}
