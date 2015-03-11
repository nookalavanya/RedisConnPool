using System;
using System.ComponentModel;
using System.ServiceModel;
using ServiceStack.Redis;
using System.Configuration;
using Meru.CDS.Common;

namespace RedisConnPool
{
    internal class MessageHandler //: IDisposable
    {
        #region Private Member Variables

        // Business object from factory.
        //IDeviceCommunication deviceCommunicationForPollingData = null;
        // Delegate to to encapsulate polling data saving method.
        private delegate void PollingDataSaveDelegate(string packet);
        // Other managed resource this class uses.
        private Component component = new Component();
        // Track whether Dispose has been called.
        private bool disposed = false;

        #endregion

        #region Private Methods

        /// <summary>
        /// Saves polling data received from device.    
        /// </summary>
        /// <param name="packet">The received packet.</param>
        private void SavePollingDataFromDevice(string packet)
        {

            // Create new delegate and encapsulate bid reply save method.
            PollingDataSaveDelegate pollingDataSaveMethodInvoker = new PollingDataSaveDelegate(SavePollingData);
            // Invoke delegate.
            pollingDataSaveMethodInvoker.Invoke(packet);

        }

        /// <summary>
        /// Saves polling data into database.
        /// </summary>
        /// <param name="packetData">The received polling data packet.</param>
        private void SavePollingData(string packetData)
        {
            try
            {
                StorePollingData(packetData);
            }
            catch (Exception ex)
            {
            }
        }

        public bool StorePollingData(string packet)
        {
            bool isPollingDataSaved = false;
            string DeviceID = "";
            double Latitude = 0.0;
            double Longitude = 0.0;
            int MeterStatus = 0;
            //int City= 0;
            string Brand = "";
            Commoncls commonCls = new Commoncls();
            ushort step = 26;
            string cityName = "";
            int cabSpeed = 0;
            int cabDirection = 0;
            int status;
            string gpsSyncValue = "";
            GeoHashConverter hash = new GeoHashConverter();
            ulong geoHash;
            RedisConn geoconverter = new RedisConn();
        
            try
            {
          
                if (!string.IsNullOrEmpty(packet) && packet.Split('|').Length >= 14)
                {
               
                    string[] pollingDataMessage = packet.Split('|');
                    DeviceID = pollingDataMessage[1].ToString();

                    GetCityBrandInformation(DeviceID, ref cityName, ref Brand);
                    Latitude = commonCls.LatLonFormate(pollingDataMessage[3].ToString());   //Convert.ToDouble(pollingDataMessage[3]);
                    Longitude = commonCls.LatLonFormate(pollingDataMessage[4].ToString());  //Convert.ToDouble(pollingDataMessage[4]);
                    MeterStatus = Convert.ToInt32(pollingDataMessage[9]);

                    long lngNextSlot;
                    //checking if the datetime string length is 12 else just skip the PD 
                    if (pollingDataMessage[7].ToString().Length < 12)
                        return false;

                    string convertedGpsDate = commonCls.GetGpsDate(pollingDataMessage[7].ToString());
                    //checking if the datetime is valid else just skip the PD 
                    if (convertedGpsDate == string.Empty)
                        return false;

                    DateTime dtGPSDate = Convert.ToDateTime(convertedGpsDate);
                    DateTime dtSlot2 = new DateTime(Convert.ToInt32(dtGPSDate.Year), Convert.ToInt32(dtGPSDate.Month), Convert.ToInt32(dtGPSDate.Day), Convert.ToInt32(dtGPSDate.Hour), Convert.ToInt32(dtGPSDate.Minute), 0);
                    lngNextSlot = ToUnixTime(dtSlot2);

                    //Getting cab speed in miles/second from PD
                    if (pollingDataMessage[5].ToString().IndexOf('.') > 0)
                    {
                        string speed = pollingDataMessage[5].ToString().Substring(0, pollingDataMessage[5].ToString().IndexOf('.'));
                        if (!string.IsNullOrEmpty(speed))
                        {
                            //this.gpsPollingData.Speed = Convert.ToInt32(speed);
                            cabSpeed = Convert.ToInt32(speed);
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(pollingDataMessage[5].ToString()))
                        {
                            //this.gpsPollingData.Speed = Convert.ToInt32(pollingDataMessage[5]);
                            cabSpeed = Convert.ToInt32(pollingDataMessage[5]);
                        }
                    }
                    //Getting cab orientation from PD
                    if (pollingDataMessage[6].ToString().IndexOf('.') > 0)
                    {
                        string direction = pollingDataMessage[6].ToString().Substring(0, pollingDataMessage[6].ToString().IndexOf('.'));
                        if (!string.IsNullOrEmpty(direction))
                        {
                            //this.gpsPollingData.Direction = Convert.ToInt32(direction);
                            cabDirection = Convert.ToInt32(direction);
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(pollingDataMessage[6].ToString()))
                        {
                            //this.gpsPollingData.Direction = Convert.ToInt32(pollingDataMessage[6]);
                            cabDirection = Convert.ToInt32(pollingDataMessage[6]);
                        }
                    }
                    //Getting cab status- Free, Hired, On call, Log out
                    status = Convert.ToInt16(pollingDataMessage[9]);
                    //Getting GPS Sync value - A or V
                    gpsSyncValue = pollingDataMessage[10].ToString();

                    geoHash = hash.geohash_encode(90, -90, 180, -180, Latitude, Longitude, step);
                    geoconverter.StoredData(cityName, Brand, geoHash, DeviceID, Latitude, Longitude, Convert.ToString(lngNextSlot), Convert.ToString(cabSpeed), Convert.ToString(cabDirection), status, gpsSyncValue);
      
                }
            }
            catch (Exception ex)
            {
                isPollingDataSaved = false;
            }

            return isPollingDataSaved;
        }

        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.
        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    component.Dispose();
                }

                // Note disposing has been done.
                disposed = true;
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Processes the received packet data and updates database.
        /// </summary>
        /// <param name="protocolPacket">The received protocol packet.</param>
        internal void ProcessReceivedData(string protocolPacket)
        {
            try
            {
                             
                string[] messageData = protocolPacket.Split('|');
                string command = messageData[0].ToString().Remove(0, 1);

                if (messageData.Length == 14)
                {
                    if (Convert.ToInt32(messageData[9]) == 0)
                    {
                    }
                }
                else
                {
                }

                if (command.ToUpper() == "PD")
                {
                    SavePollingDataFromDevice(protocolPacket);
                }
            }
            catch (Exception ex)
            {
            
            }
        }

        public void GetCityBrandInformation(string DeviceID, ref string cityName, ref string Brand)
        {
            //kuala lumpur
            if (DeviceID == "2350001")
            {
                Brand = "Meru";
                cityName = "KualaLumpur";
            }
            else if ((DeviceID.Length == 7) && ((DeviceID.Substring(0, 3) == "411") || (DeviceID.Substring(0, 3) == "415")))
            {
                Brand = "Genie";
                cityName = "Hyderabad";
            }
            else if ((DeviceID.Length == 7) && ((DeviceID.Substring(0, 3) == "811") || (DeviceID.Substring(0, 3) == "815")))
            {
                Brand = "Genie";
                cityName = "Bangalore";
            }
            else if ((DeviceID.Length == 7) && ((DeviceID.Substring(0, 3) == "211") || (DeviceID.Substring(0, 3) == "215")))
            {
                Brand = "Genie";
                cityName = "Pune";
            }
            else if ((DeviceID.Length == 7) && ((DeviceID.Substring(0, 3) == "151") || (DeviceID.Substring(0, 3) == "155")))
            {
                Brand = "Genie";
                cityName = "Delhi";
            }
            else if ((DeviceID.Length == 7) && ((DeviceID.Substring(0, 3) == "445") || (DeviceID.Substring(0, 3) == "441")))
            {
                Brand = "Meru";
                cityName = "Chennai";
            }
            else if ((DeviceID.Length == 7) && ((DeviceID.Substring(0, 3) == "795") || (DeviceID.Substring(0, 3) == "791")))
            {
                Brand = "Meru";
                cityName = "Ahmedabad";
            }
            else if ((DeviceID.Length == 7) && ((DeviceID.Substring(0, 3) == "141") || (DeviceID.Substring(0, 3) == "145")))
            {
                Brand = "Meru";
                cityName = "Jaipur";
            }
            else if (((DeviceID.Length == 6) && (DeviceID.Substring(0, 2) == "10")) || ((DeviceID.Length == 6) && (DeviceID.Substring(0, 2) == "70")) || ((DeviceID.Length == 7) && ((DeviceID.Substring(0, 3) == "221") || (DeviceID.Substring(0, 3) == "225"))))
            {
                Brand = "Meru";
                cityName = "Mumbai";
            }
            else if (((DeviceID.Length == 6) && (DeviceID.Substring(0, 1) == "3")) || ((DeviceID.Length == 7) && ((DeviceID.Substring(0, 3) == "111") || (DeviceID.Substring(0, 3) == "115"))))
            {
                Brand = "Meru";
                cityName = "Delhi";
            }
            else if (((DeviceID.Length == 6) && (DeviceID.Substring(0, 1) == "4")) || ((DeviceID.Length == 7) && ((DeviceID.Substring(0, 3) == "401") || (DeviceID.Substring(0, 3) == "405"))))
            {
                Brand = "Meru";
                cityName = "Hyderabad";
            }
            else if (((DeviceID.Length == 6) && (DeviceID.Substring(0, 1) == "5")) || ((DeviceID.Length == 7) && ((DeviceID.Substring(0, 3) == "805") || (DeviceID.Substring(0, 3) == "801"))))
            {
                Brand = "Meru";
                cityName = "Bangalore";
            }
            else if ((DeviceID.Length == 7) && (DeviceID.Substring(0, 2) == "33"))
            {
                Brand = "Meru";
                cityName = "Kolkata";
            }
            else if ((DeviceID.Length == 7) && (DeviceID.Substring(0, 2) == "65"))
            {
                Brand = "Meru";
                cityName = "Vadodara";
            }
            else if ((DeviceID.Length == 7) && (DeviceID.Substring(0, 2) == "20"))
            {
                Brand = "Meru";
                cityName = "Pune";
            }
            else if ((DeviceID.Length == 7) && (DeviceID.Substring(0, 2) == "45"))
            {
                Brand = "Genie";
                cityName = "Chennai";
            }
            else if ((DeviceID.Length == 6) && (DeviceID.Substring(0, 2) == "22"))
            {
                Brand = "MeruFlexi";
                cityName = "Mumbai";
            }
            else if ((DeviceID.Length == 6) && (DeviceID.Substring(0, 2) == "11"))
            {
                Brand = "MeruFlexi";
                cityName = "Delhi";
            }
            else if ((DeviceID.Length == 6) && (DeviceID.Substring(0, 2) == "80"))
            {
                Brand = "MeruFlexi";
                cityName = "Bangalore";
            }
            else if ((DeviceID.Length == 6) && (DeviceID.Substring(0, 1) == "9"))
            {
                Brand = "MeruFlexi";
                cityName = "Hyderabad";
            }
            //Chandigarh 
            else if ((DeviceID.Length == 7) && (DeviceID.Substring(0, 2) == "72"))
            {
                Brand = "Meru";
                cityName = "Chandigarh";
            }
            //Chandigarh Genie
            else if ((DeviceID.Length == 7) && (DeviceID.Substring(0, 2) == "71"))
            {
                Brand = "Genie";
                cityName = "Chandigarh";
            }
            //Mumbai Genie
            else if ((DeviceID.Length == 7) && (DeviceID.Substring(0, 2) == "23"))
            {
                Brand = "Genie";
                cityName = "Mumbai";
            }
            //Visakhapatnam
            else if ((DeviceID.Length == 7) && (DeviceID.Substring(0, 2) == "89"))
            {
                Brand = "Meru";
                cityName = "Visakhapatnam";
            }
            //Visakhapatnam Genie
            else if ((DeviceID.Length == 7) && (DeviceID.Substring(0, 2) == "88"))
            {
                Brand = "Genie";
                cityName = "Visakhapatnam";
            }
            //Surat
            else if ((DeviceID.Length == 7) && (DeviceID.Substring(0, 3) == "611"))
            {
                Brand = "Meru";
                cityName = "Surat";
            }
            else if ((DeviceID.Length == 7) && (DeviceID.Substring(0, 3) == "461"))
            {
                Brand = "Genie";
                cityName = "Ahmedabad";
            }
            else if ((DeviceID.Length == 7) && (DeviceID.Substring(0, 3) == "471"))
            {
                Brand = "Meru";
                cityName = "Bhubaneswar";
            }
            else if ((DeviceID.Length == 7) && ((DeviceID.Substring(0, 3) == "521") || (DeviceID.Substring(0, 3) == "525")))
            {
                Brand = "MeruEve";
                cityName = "Delhi";
            }
            else if ((DeviceID.Length == 7) && (DeviceID.Substring(0, 3) == "541"))
            {
                Brand = "Genie";
                cityName = "Surat";
            }
            else if ((DeviceID.Length == 7) && (DeviceID.Substring(0, 3) == "481"))
            {
                Brand = "Meru";
                cityName = "Mysore";
            }
            else if ((DeviceID.Length == 7) && (DeviceID.Substring(0, 3) == "491"))
            {
                Brand = "Genie";
                cityName = "Mysore";
            }
            else if ((DeviceID.Length == 7) && (DeviceID.Substring(0, 2) == "55"))
            {
                Brand = "Meru";
                cityName = "Jodhpur";
            }
            else if ((DeviceID.Length == 7) && (DeviceID.Substring(0, 2) == "53"))
            {
                Brand = "Meru";
                cityName = "Udaipur";
            }
            else if ((DeviceID.Length == 7) && (DeviceID.Substring(0, 2) == "56"))
            {
                Brand = "Meru";
                cityName = "Indore";
            }
            else if ((DeviceID.Length == 7) && (DeviceID.Substring(0, 2) == "57"))
            {
                Brand = "Meru";
                cityName = "Ludhiana";
            }
            else if ((DeviceID.Length == 7) && (DeviceID.Substring(0, 2) == "63"))
            {
                Brand = "Genie";
                cityName = "Indore";
            }
            else
            {
              
            }
        }

        public long ToUnixTime(DateTime date)
        {
            long timeinSeconds = 0;
            try
            {
                var epoch = new DateTime(1970, 1, 1, 5, 30, 0, DateTimeKind.Local);
                timeinSeconds = Convert.ToInt64((date - epoch).TotalSeconds);
            }
            catch { }
            finally { }
            return timeinSeconds;
        }

        public double GetRadialDistance(double lat1, double lon1, string cityName, char unit, int isIntl)
        {
            double lat2 = 0;
            double lon2 = 0;

            if (cityName == "Hyderabad")
            {
                lat2 = Convert.ToDouble(GeoFence.HydLat);
                lon2 = Convert.ToDouble(GeoFence.HydLng);
            }
            else if ((cityName == "Delhi") && (isIntl == 0))
            {
                lat2 = Convert.ToDouble(GeoFence.DelLat);
                lon2 = Convert.ToDouble(GeoFence.DelLng);
            }
            else if ((cityName == "Delhi") && (isIntl == 1))
            {
                lat2 = Convert.ToDouble(GeoFence.DelIntLat);
                lon2 = Convert.ToDouble(GeoFence.DelIntLng);
            }
            else if (cityName == "Bangalore")
            {
                lat2 = Convert.ToDouble(GeoFence.BngLat);
                lon2 = Convert.ToDouble(GeoFence.BngLng);
            }
            else if ((cityName == "Mumbai") && (isIntl == 0))
            {
                lat2 = Convert.ToDouble(GeoFence.MumLat);
                lon2 = Convert.ToDouble(GeoFence.MumLng);
            }
            else if ((cityName == "Mumbai") && (isIntl == 1))
            {
                lat2 = Convert.ToDouble(GeoFence.MumIntLat);
                lon2 = Convert.ToDouble(GeoFence.MumIntLng);
            }
            double theta = lon1 - lon2;
            double dist = Math.Sin(deg2rad(lat1)) * Math.Sin(deg2rad(lat2)) + Math.Cos(deg2rad(lat1)) * Math.Cos(deg2rad(lat2)) * Math.Cos(deg2rad(theta));
            dist = Math.Acos(dist);
            dist = rad2deg(dist);
            dist = dist * 60 * 1.1515;
            if (unit == 'K')
            {
                dist = dist * 1.609344;
            }
            else if (unit == 'N')
            {
                dist = dist * 0.8684;
            }
            return (dist);
        }

        private double deg2rad(double deg)
        {
            return (deg * Math.PI / 180.0);
        }

        private double rad2deg(double rad)
        {
            return (rad / Math.PI * 180.0);
        }

        public struct GeoFence
        {
            public const double HydLat = 17.23984;
            public const double HydLng = 78.42625;
            public const double BngLat = 13.20276;
            public const double BngLng = 77.69454;
            public const double DelIntLat = 28.55754;
            public const double DelIntLng = 77.08814;
            public const double DelLat = 28.56168;
            public const double DelLng = 77.119;
            public const double MumLat = 19.09327;
            public const double MumLng = 72.85497;
            public const double MumIntLat = 19.09657;
            public const double MumIntLng = 72.87585;
       }
        #endregion

        #region IDisposable Members

        // Implement IDisposable.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        /// <summary>
        /// All managed and unmanaged resources will be disposed.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}

