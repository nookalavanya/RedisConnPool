using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.Redis;
using System.ServiceModel;
using System.ServiceModel.MsmqIntegration;
using System.Configuration;

namespace RedisConnPool
{
    class Program
    {
        static void Main(string[] args)
        {
            for (int i = 0; i < 1000; i++)
            {
                string deviceId = "41100";
                deviceId = (Convert.ToInt32(deviceId) + i).ToString();
                string packet = "[PD|"+deviceId+"|76|1907.707529999998|07253.740969999998|0.23|189.73|104731260814|1|0|A|1|88.64|3]";
                MsmqMessage<string> packet1 = new MsmqMessage<string>(packet);
                SaveReceivedData(packet1);
            }
         }

        public static void SaveReceivedData(MsmqMessage<string> receivedPacket)
        {
            // Create MessageHandler objects.
            MessageHandler messageHandlerForPollingData = new MessageHandler();

            try
            {
                // Get protocol data from MSMQ message body. 
                string packet = receivedPacket.Body;

                if (!string.IsNullOrEmpty(packet))
                {
                    try
                    {
                        messageHandlerForPollingData.ProcessReceivedData(packet);
                    }

                    catch (Exception ex)
                    {
                        
                    }
                }
            }
            catch (Exception ex)
            {
            
            }
        }

    }
}
