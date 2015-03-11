using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisConnPool
{
    class GeoHashConverter
    {
        public UInt64 geohash_encode(double lat_max, double lat_min, double lon_max, double lon_min, double latitude, double longitude, UInt16 step)
        {

            GeoHashRange lat_range = new GeoHashRange();
            GeoHashRange lon_range = new GeoHashRange();
            //lat_range, lon_range;
            lat_range.Max = lat_max;
            lat_range.Min = lat_min;
            lon_range.Max = lon_max;
            lon_range.Min = lon_min;
            GeoHashBits hash = new GeoHashBits();
            hash.Bits = 0;
            hash.Step = step;
            UInt16 i = 0;

            for (; i < step; i++)
            {
                UInt16 lat_bit, lon_bit;
                if (lat_range.Max - latitude >= latitude - lat_range.Min)
                {
                    lat_bit = 0;
                    lat_range.Max = (lat_range.Max + lat_range.Min) / 2;
                }
                else
                {
                    lat_bit = 1;
                    lat_range.Min = (lat_range.Max + lat_range.Min) / 2;
                }
                if (lon_range.Max - longitude >= longitude - lon_range.Min)
                {
                    lon_bit = 0;
                    lon_range.Max = (lon_range.Max + lon_range.Min) / 2;
                }
                else
                {
                    lon_bit = 1;
                    lon_range.Min = (lon_range.Max + lon_range.Min) / 2;
                }
                hash.Bits <<= 1;
                hash.Bits += lon_bit;
                hash.Bits <<= 1;
                hash.Bits += lat_bit;
            }
            return hash.Bits;
        }

        enum GeoDirection
        {
            GEOHASH_NORTH = 0,
            GEOHASH_EAST,
            GEOHASH_WEST,
            GEOHASH_SOUTH,
            GEOHASH_SOUTH_WEST,
            GEOHASH_SOUTH_EAST,
            GEOHASH_NORT_WEST,
            GEOHASH_NORT_EAST
        }

        struct GeoHashBits
        {
            UInt64 bits;

            public UInt64 Bits
            {
                get { return bits; }
                set { bits = value; }
            }
            UInt16 step;

            public UInt16 Step
            {
                get { return step; }
                set { step = value; }
            }
        }
        struct GeoHashRange
        {
            double max;

            public double Max
            {
                get { return max; }
                set { max = value; }
            }
            double min;

            public double Min
            {
                get { return min; }
                set { min = value; }
            }
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
            //public const double AhmLat = 23.07176;
            //public const double AhmLng = 72.61705;
            //public const double AhmIntLat = 23.07713;
            //public const double AhmIntLng = 72.63465;
            //public const double JaiLat = 26.83014;
            //public const double JaiLng = 75.80504;
            //public const double PuneLat = 18.58378;
            //public const double PuneLng = 73.91902;
            //public const double ChnLat = 12.98193;
            //public const double ChnLng = 80.16204;
        }
    }
}
