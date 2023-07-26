using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Remoting.Messaging;
using System.Runtime.InteropServices.ComTypes;

class Program
{
    // Define a class to represent the vehicle data
    class VehicleData
    {
        public int VehicleId { get; set; }
        public string VehicleRegistration { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public ulong RecordedTimeUTC { get; set; }
    }

    static void Main()
    {
        // Replace "path/to/your/vehicles.dat" with the actual path to the binary data file.
        string dataFilePath = "C:/Users/ngcoy/OneDrive/Desktop/VehiclePositions.dat";
        string outputFilename = "C:/Users/ngcoy/OneDrive/Desktop/VehiclePositions.txt";

        // Define the 10 coordinates to find the closest vehicle positions to.
        var coordinatesToFind = new List<(double, double)>
        {
            (34.544909, -102.100843),
            (32.345544, -99.123124),
            (33.234235, -100.214124),
            (35.195739, -95.348899),
            (31.895839, -97.789573),
            (32.895839, -101.789573),
            (34.115839, -100.225732),
            (32.335839, -99.992232),
            (33.535339, -94.792232),
            (32.234235, -100.222222)
        };

        try
        {
            // Read the vehicle data from the binary file
            List<VehicleData> vehicleData = ReadVehicleDataFromBinaryFile(dataFilePath, outputFilename);

            StringBuilder sb = new StringBuilder();

            string vehicleId, vehicleRegistration, latitude, longitude;

            // Build the Quadtree using the vehicle data
            Quadtree quadtree = new Quadtree();
            foreach (var vehicle in vehicleData)
            {
                quadtree.Insert(vehicle.Latitude, vehicle.Longitude, vehicle);
            }

            // Find the closest vehicle positions for each of the provided coordinates
            foreach (var coordinate in coordinatesToFind)
            {
                VehicleData closestVehicle = quadtree.FindNearest(coordinate.Item1, coordinate.Item2);
                Console.WriteLine($"Closest vehicle for Latitude: {coordinate.Item1}, Longitude: {coordinate.Item2}");
                Console.WriteLine($"Vehicle ID: {closestVehicle.VehicleId}, Registration: {closestVehicle.VehicleRegistration}");

                vehicleId = closestVehicle.VehicleId.ToString();
                vehicleRegistration = closestVehicle.VehicleRegistration.ToString();
                latitude = coordinate.Item1.ToString();
                longitude = coordinate.Item2.ToString();

                sb.AppendLine($"Closest vehicle for Latitude: {coordinate.Item1}, Longitude: {coordinate.Item2}");
                sb.AppendLine($"Vehicle ID: {closestVehicle.VehicleId}, Registration: {closestVehicle.VehicleRegistration}");
                //sb.AppendLine(vehicleId + " | " + vehicleRegistration + " | " + latitude + " | " + longitude);
            }
            File.WriteAllText(outputFilename, sb.ToString());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    static List<VehicleData> ReadVehicleDataFromBinaryFile(string filePath, string outfile)
    {
        List<VehicleData> vehicleDataList = new List<VehicleData>();
        //StringBuilder sb = new StringBuilder();

        //string vehicleId, vehicleRegistration, latitude, longitude, recordedTimeUTC;

        using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
        {
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            //while (reader.BaseStream.Position < reader.BaseStream.Length -10)
            //while (reader.BaseStream.Position < 62001433)
            {
                VehicleData vehicleData = new VehicleData
                {
                    VehicleId = reader.ReadInt32(),
                    VehicleRegistration = ReadNullTerminatedString(reader),
                    Latitude = reader.ReadSingle(),
                    Longitude = reader.ReadSingle(),
                    RecordedTimeUTC = reader.ReadUInt64()
                };

                //vehicleId = reader.ReadInt32().ToString();
                //vehicleRegistration = ReadNullTerminatedString(reader);
                //latitude = reader.ReadSingle().ToString();
                //longitude = reader.ReadSingle().ToString();
                //recordedTimeUTC = reader.ReadUInt64().ToString();

                vehicleDataList.Add(vehicleData);
                //sb.AppendLine(vehicleId + " | " + vehicleRegistration + " | " + latitude + " | " + longitude + " | " + recordedTimeUTC);
            }
        }

        //File.WriteAllText(outfile, sb.ToString());
        return vehicleDataList;
    }

    static string ReadNullTerminatedString(BinaryReader reader)
    {
        List<byte> bytes = new List<byte>();
        byte currentByte;
        while ((currentByte = reader.ReadByte()) != 0)
        {
            bytes.Add(currentByte);
        }
        return System.Text.Encoding.ASCII.GetString(bytes.ToArray());
    }

    // Quadtree implementation
    class Quadtree
    {
        private const int MaxCapacity = 4;

        private QuadtreeNode root;

        public Quadtree()
        {
            root = new QuadtreeNode();
        }

        public void Insert(double latitude, double longitude, VehicleData data)
        {
            root.Insert(latitude, longitude, data);
        }

        public VehicleData FindNearest(double latitude, double longitude)
        {
            return root.FindNearest(latitude, longitude);
        }

        // Quadtree node class
        private class QuadtreeNode
        {
            private const int MaxDepth = 10;

            private List<VehicleData> vehicleDataList;
            private QuadtreeNode[] children;
            private double centerX;
            private double centerY;
            private double halfWidth;
            private int depth;

            public QuadtreeNode()
            {
                vehicleDataList = new List<VehicleData>();
                children = null;
                depth = 0;
            }

            private QuadtreeNode(double centerX, double centerY, double halfWidth, int depth)
            {
                vehicleDataList = new List<VehicleData>();
                children = null;
                this.centerX = centerX;
                this.centerY = centerY;
                this.halfWidth = halfWidth;
                this.depth = depth;
            }

            public void Insert(double latitude, double longitude, VehicleData data)
            {
                if (children != null)
                {
                    int quadrant = GetQuadrant(latitude, longitude);
                    children[quadrant].Insert(latitude, longitude, data);
                }
                else
                {
                    vehicleDataList.Add(data);
                    if (vehicleDataList.Count > MaxCapacity && depth < MaxDepth)
                    {
                        Split();
                    }
                }
            }

            private void Split()
            {
                double quarterWidth = halfWidth / 2.0;
                children = new QuadtreeNode[4];
                children[0] = new QuadtreeNode(centerX - quarterWidth, centerY + quarterWidth, quarterWidth, depth + 1);
                children[1] = new QuadtreeNode(centerX + quarterWidth, centerY + quarterWidth, quarterWidth, depth + 1);
                children[2] = new QuadtreeNode(centerX - quarterWidth, centerY - quarterWidth, quarterWidth, depth + 1);
                children[3] = new QuadtreeNode(centerX + quarterWidth, centerY - quarterWidth, quarterWidth, depth + 1);

                foreach (var data in vehicleDataList)
                {
                    int quadrant = GetQuadrant(data.Latitude, data.Longitude);
                    children[quadrant].Insert(data.Latitude, data.Longitude, data);
                }

                vehicleDataList.Clear();
            }

            public VehicleData FindNearest(double latitude, double longitude)
            {
                VehicleData nearestData = null;
                double nearestDistanceSquared = double.MaxValue;

                if (children != null)
                {
                    int quadrant = GetQuadrant(latitude, longitude);
                    nearestData = children[quadrant].FindNearest(latitude, longitude);
                    double nearestDistance = GetDistanceSquared(latitude, longitude, nearestData.Latitude, nearestData.Longitude);
                    if (nearestDistance < nearestDistanceSquared)
                    {
                        nearestDistanceSquared = nearestDistance;
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        if (i != quadrant)
                        {
                            double distanceToBoundary = GetDistanceToBoundary(latitude, longitude, i);
                            if (distanceToBoundary < nearestDistanceSquared)
                            {
                                VehicleData tempData = children[i].FindNearest(latitude, longitude);
                                double tempDistance = GetDistanceSquared(latitude, longitude, tempData.Latitude, tempData.Longitude);
                                if (tempDistance < nearestDistanceSquared)
                                {
                                    nearestData = tempData;
                                    nearestDistanceSquared = tempDistance;
                                }
                            }
                        }
                    }
                }
                else
                {
                    foreach (var data in vehicleDataList)
                    {
                        double distanceSquared = GetDistanceSquared(latitude, longitude, data.Latitude, data.Longitude);
                        if (distanceSquared < nearestDistanceSquared)
                        {
                            nearestData = data;
                            nearestDistanceSquared = distanceSquared;
                        }
                    }
                }

                return nearestData;
            }

            private int GetQuadrant(double latitude, double longitude)
            {
                if (latitude >= centerY)
                {
                    if (longitude < centerX)
                        return 0;
                    else
                        return 1;
                }
                else
                {
                    if (longitude < centerX)
                        return 2;
                    else
                        return 3;
                }
            }

            private double GetDistanceSquared(double lat1, double lon1, double lat2, double lon2)
            {
                double latDiff = lat2 - lat1;
                double lonDiff = lon2 - lon1;
                return latDiff * latDiff + lonDiff * lonDiff;
            }

            private double GetDistanceToBoundary(double latitude, double longitude, int quadrant)
            {
                double dx = 0, dy = 0;
                if (quadrant == 0 || quadrant == 2)
                {
                    dx = centerX - longitude;
                }
                else
                {
                    dx = longitude - centerX;
                }

                if (quadrant == 0 || quadrant == 1)
                {
                    dy = centerY - latitude;
                }
                else
                {
                    dy = latitude - centerY;
                }

                return dx * dx + dy * dy;
            }
        }
    }
}