using System;
using System.Collections.Generic;
using System.Linq;
using CallbackFS;

namespace Crypto.Runner
{
    using System.IO;
    using ACCESS_MASK = System.UInt32;

    class Program
    {
        private const string driverPath = @"..\..\..\..\..\Drivers\cbfs.cab";
        private const string mRootPath = @"C:\1";
        private const string mGuid = "713CC6CE-B3E2-4fd9-838D-E28F558F6866";
        private const string mRegKey7 = "2A65D30F47C829E1FF5CB13E4300B5A23798B17A3104CB490D3ADF3CFB8DD67236152C722D39FC244BA7B3832BC18E7696B68894075485F0259277087D2A4F182D7E83784D5E631C51FA9FBC319A3F98ADFE03CC81AACFBC319A3F9CF17E83F180";
        private const string mRegKey6 = "3C9A3DCC0A8355554D7A1F7CD15E6320B5CE84CF545342A670A512F7FE76D71DAB4625E1944A157766442EC01E4A2F691B7511F7BA2F6017B44916476479C6BF3CB5A23FBC3522BB98A976FBD8E9B6FF7CF5E24BE879C69BB889569B988DBA87BE";

        static void Main(string[] args)
        {
            CallbackFileSystem cbfs = null;
            UInt32 Reboot = 0;
            try
            {
                //cbfs = new CallbackFileSystem
                //{
                //    OnGetVolumeLabel = CbFsGetVolumeLabel,
                //    OnCreateFile = CbFsCreateFile,
                //    OnMount = x => { }
                //};
                cbfs = GetCallbackFileSystem(mRootPath);


                var status = UpdateDriverStatus();
                Console.WriteLine(status.Item2);

                if (!status.Item1)
                {
                    Console.WriteLine("Installing");
                    CallbackFileSystem.Install(driverPath, mGuid, Environment.SystemDirectory, true,
                                                   CallbackFileSystem.CBFS_MODULE_NET_REDIRECTOR_DLL /*|
                                                    CallbackFileSystem.CBFS_MODULE_MOUNT_NOTIFIER_DLL*/,
                                                   ref Reboot);
                }


                // CREATE STORAGE
                CallbackFileSystem.SetRegistrationKey(mRegKey6);
                CallbackFileSystem.Initialize(mGuid);
                cbfs.CreateStorage();

                
                // MOUNT
                //
                // Use this method to mount new media to the created storage. 
                // Call this method after calling CreateStorage. 
                // For non-PnP storages you can add mounting points before or after calling MountMedia. 
                // For PnP storages you need to call MountMedia before adding any mounting points.
                Console.WriteLine("Mounting...");
                cbfs.MountMedia(0); // chyba nie potrzebne
                Console.WriteLine(UpdateMountingPoints(cbfs));


                // ADD mounting point
                var dirinfo = new DirectoryInfo(mRootPath);
                if (!dirinfo.Exists)
                {
                    dirinfo.Create();
                }
                cbfs.AddMountingPoint("X:", CallbackFileSystem.CBFS_SYMLINK_MOUNT_MANAGER, null);



                Console.WriteLine("Press any key for Disposal...");
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e}");
            }
            finally
            {
                if (null != cbfs)
                {
                    // rzuca wyjątek - kolejność do ustalenia
                    // cbfs.UnmountMedia(true); - nie potrzebne
                    while (cbfs.GetMountingPointCount() != 0)
                    {
                        cbfs.DeleteMountingPoint(0);
                    }
                    cbfs.DeleteStorage(true);
                }

                // CallbackFileSystem.Uninstall(driverPath, mGuid, Environment.SystemDirectory, ref Reboot);

                var status = UpdateDriverStatus();
                Console.WriteLine(status);
            }

            Console.WriteLine("Press any key for exit...");
            Console.ReadLine();
        }

        private static CallbackFileSystem GetCallbackFileSystem(string rootPath)
        {
            return new MyFileSystem(mRootPath).CallbackFileSystem;
        }

        private static Tuple<bool, string> UpdateDriverStatus()
        {
            bool Installed = false;

            int VersionHigh = 0, VersionLow = 0;

            SERVICE_STATUS status = new SERVICE_STATUS();

            CallbackFileSystem.GetModuleStatus(mGuid, CallbackFileSystem.CBFS_MODULE_DRIVER, ref Installed, ref VersionHigh, ref VersionLow, ref status);

            string message;
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

                message = string.Format("Driver (ver {0}.{1}.{2}.{3}) installed, service {4}", VersionHigh >> 16, VersionHigh & 0xFFFF, VersionLow >> 16, VersionLow & 0xFFFF, strStat);
            }
            else
            {
                message = "Driver not installed";
            }

            return Tuple.Create(Installed, message);
        }
        private static string UpdateMountingPoints(CallbackFileSystem cbfs)
        {
            int Index;
            string MountingPoint;
            uint Flags;
            LUID AuthenticationId;

            var lstPoints = new List<string>();

            for (Index = 0; Index < cbfs.MountingPointCount; Index++)
            {
                cbfs.GetMountingPoint(Index, out MountingPoint, out Flags, out AuthenticationId);
                lstPoints.Add(MountingPoint);

            }
            if (lstPoints.Count > 0)
            {
                //lstPoints.SetSelected(0, true);
            }

            return string.Join(", ", lstPoints);
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

        private static void CbFsGetVolumeLabel(object sender, ref string VolumeLabel)
        {
            VolumeLabel = "CbFs Test";
        }
    }
}
