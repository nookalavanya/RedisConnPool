using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.Redis;
using System.Configuration;

namespace RedisConnPool
{
    class RedisConn
    {
        public void StoredData(string CityName, string Brand, ulong geoHash, string DeviceId, double Latitude, double Longitude, string convertedGpsDate, string speed, string direction, int status, string gpsSyncValue)
        {
            try
            {
                string setId = "Location." + CityName + "." + Brand;
                string hashId = "status." + DeviceId;

                //*Connection Pooling*//
                PooledRedisClientManager pooledClientManager = new PooledRedisClientManager(50, 5, "localhost");
                pooledClientManager.PoolTimeout = 10;
                pooledClientManager.IdleTimeOutSecs = 3;
                //*Connection Pooling*//
  
                using (var client = pooledClientManager.GetClient())
                {
                    //Pushing PD into Redis only if all the Device id, Cityname annd Brand have values from PD
                    if (!client.HadExceptions)
                    {
                        if (CityName != "" && DeviceId != "" && Brand != "")
                        {
                            //Inserting only free cabs as sorted sets and GPSSyncValue = 'A' - This is avoid filtering on free cabs when cabs are fetched for a pick up
                            if ((status == 1) && (gpsSyncValue == "A"))
                            {
                                //creating connection with the Redis client
                                try
                                {
                                    client.AddItemToSortedSet(setId, DeviceId, geoHash);
                                   
                                }
                                catch (Exception ex)
                                {
                                   
                                }
                            }
                            //Removing the device id sorted set if the cab meter status is not free or GPS lost
                            else if ((status != 1) || (gpsSyncValue == "V"))
                            {
                                try
                                {
                                    client.RemoveItemFromSortedSet(setId, DeviceId);
                            
                                }
                                catch (Exception ex)
                                {
                                
                                }
                            }

                            //Adding Device Id details to the REDIS irrespective of cab status being free - this is needed for tracking the cabs
                            try
                            {
                                client.SetEntryInHash(hashId, "lat", Convert.ToString(Latitude));
                                client.SetEntryInHash(hashId, "lng", Convert.ToString(Longitude));
                                client.SetEntryInHash(hashId, "location_ts", convertedGpsDate);
                                client.SetEntryInHash(hashId, "status", Convert.ToString(status));
                                client.SetEntryInHash(hashId, "speed_mps", speed);
                                client.SetEntryInHash(hashId, "orientation", direction);
                            }
                            catch (Exception ex)
                            {
                               
                            }

                        }
                    }
                    else
                    {
                        
                    }
                }
            }

            catch (Exception ex)
            {
            
            }
            finally
            {

            }

        }      
    }
}
