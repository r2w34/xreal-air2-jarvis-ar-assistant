using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text.RegularExpressions;

namespace XREALJarvis.Maps
{
    /// <summary>
    /// Manages map integration and navigation for the Jarvis AR Assistant
    /// Handles GPS location, map rendering, and navigation overlays
    /// </summary>
    public class MapManager : MonoBehaviour
    {
        [Header("API Configuration")]
        [SerializeField] private string googleMapsAPIKey = "";
        [SerializeField] private string mapboxAPIKey = "";
        [SerializeField] private bool useGoogleMaps = true;

        [Header("Map Settings")]
        [SerializeField] private int mapWidth = 512;
        [SerializeField] private int mapHeight = 512;
        [SerializeField] private int defaultZoom = 15;
        [SerializeField] private string mapType = "roadmap"; // roadmap, satellite, hybrid, terrain

        [Header("Location Settings")]
        [SerializeField] private float locationUpdateInterval = 5f;
        [SerializeField] private float locationAccuracy = 10f;
        [SerializeField] private bool enableLocationServices = true;

        [Header("Navigation")]
        [SerializeField] private Material navigationArrowMaterial;
        [SerializeField] private GameObject navigationArrowPrefab;
        [SerializeField] private float arrowDistance = 2f;
        [SerializeField] private float arrowHeight = 0.5f;

        // Events
        public Action<Texture2D> OnMapImageReceived;
        public Action<NavigationData> OnNavigationDataReceived;
        public Action<LocationData> OnLocationUpdated;
        public Action<string> OnMapError;

        // State
        private LocationData currentLocation;
        private NavigationData currentNavigation;
        private bool isLocationServiceRunning = false;
        private bool isNavigating = false;
        private GameObject currentNavigationArrow;

        // Map rendering
        private Texture2D currentMapTexture;
        private Renderer mapRenderer;

        [System.Serializable]
        public class LocationData
        {
            public float latitude;
            public float longitude;
            public float altitude;
            public float accuracy;
            public float timestamp;
            public string address;

            public LocationData(float lat, float lon, float alt = 0f, float acc = 0f)
            {
                latitude = lat;
                longitude = lon;
                altitude = alt;
                accuracy = acc;
                timestamp = Time.time;
            }

            public override string ToString()
            {
                return $"Lat: {latitude:F6}, Lon: {longitude:F6}";
            }
        }

        [System.Serializable]
        public class NavigationData
        {
            public LocationData destination;
            public string destinationAddress;
            public float distance;
            public float duration;
            public string[] instructions;
            public LocationData[] waypoints;
            public float bearing; // Direction to destination in degrees

            public NavigationData(LocationData dest, string address)
            {
                destination = dest;
                destinationAddress = address;
            }
        }

        public void Initialize()
        {
            StartCoroutine(InitializeLocationServices());
            LoadAPIKeys();
        }

        private void LoadAPIKeys()
        {
            // Load API keys from config
            string configPath = System.IO.Path.Combine(Application.streamingAssetsPath, "config.json");
            
            if (System.IO.File.Exists(configPath))
            {
                try
                {
                    string configJson = System.IO.File.ReadAllText(configPath);
                    var config = JsonUtility.FromJson<MapConfig>(configJson);
                    googleMapsAPIKey = config.google_maps_api_key;
                    mapboxAPIKey = config.mapbox_api_key;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MapManager] Failed to load API keys: {e.Message}");
                }
            }

            // Fallback to PlayerPrefs
            if (string.IsNullOrEmpty(googleMapsAPIKey))
                googleMapsAPIKey = PlayerPrefs.GetString("GOOGLE_MAPS_API_KEY", "");
            if (string.IsNullOrEmpty(mapboxAPIKey))
                mapboxAPIKey = PlayerPrefs.GetString("MAPBOX_API_KEY", "");
        }

        private IEnumerator InitializeLocationServices()
        {
            if (!enableLocationServices)
            {
                Debug.Log("[MapManager] Location services disabled");
                yield break;
            }

            // Check if location services are enabled
            if (!Input.location.isEnabledByUser)
            {
                Debug.LogWarning("[MapManager] Location services not enabled by user");
                OnMapError?.Invoke("Location services not enabled. Please enable GPS.");
                yield break;
            }

            // Start location service
            Input.location.Start(locationAccuracy, locationAccuracy);

            // Wait for initialization
            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return new WaitForSeconds(1);
                maxWait--;
            }

            // Check if initialization was successful
            if (maxWait < 1)
            {
                Debug.LogError("[MapManager] Location service initialization timed out");
                OnMapError?.Invoke("Location service initialization timed out");
                yield break;
            }

            if (Input.location.status == LocationServiceStatus.Failed)
            {
                Debug.LogError("[MapManager] Unable to determine device location");
                OnMapError?.Invoke("Unable to determine device location");
                yield break;
            }

            // Location service initialized successfully
            isLocationServiceRunning = true;
            Debug.Log("[MapManager] Location services initialized");

            // Start location updates
            StartCoroutine(UpdateLocationCoroutine());
        }

        private IEnumerator UpdateLocationCoroutine()
        {
            while (isLocationServiceRunning)
            {
                if (Input.location.status == LocationServiceStatus.Running)
                {
                    var locationInfo = Input.location.lastData;
                    currentLocation = new LocationData(
                        locationInfo.latitude,
                        locationInfo.longitude,
                        locationInfo.altitude,
                        locationInfo.horizontalAccuracy
                    );

                    OnLocationUpdated?.Invoke(currentLocation);
                    Debug.Log($"[MapManager] Location updated: {currentLocation}");
                }

                yield return new WaitForSeconds(locationUpdateInterval);
            }
        }

        public void ProcessMapRequest(string request)
        {
            Debug.Log($"[MapManager] Processing map request: {request}");

            // Extract location/destination from request
            string destination = ExtractDestinationFromRequest(request);
            
            if (!string.IsNullOrEmpty(destination))
            {
                StartCoroutine(GetDirections(destination));
            }
            else
            {
                // Show current location map
                ShowCurrentLocationMap();
            }
        }

        private string ExtractDestinationFromRequest(string request)
        {
            // Simple regex patterns to extract destinations
            string[] patterns = {
                @"(?:navigate to|directions to|go to|find)\s+(.+)",
                @"(?:where is|location of)\s+(.+)",
                @"(?:show me|find)\s+(.+?)(?:\s+(?:on map|map))?$"
            };

            foreach (string pattern in patterns)
            {
                Match match = Regex.Match(request, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
            }

            return "";
        }

        public void ShowCurrentLocationMap()
        {
            if (currentLocation == null)
            {
                OnMapError?.Invoke("Current location not available");
                return;
            }

            StartCoroutine(LoadStaticMap(currentLocation.latitude, currentLocation.longitude));
        }

        private IEnumerator LoadStaticMap(float latitude, float longitude, string markers = "")
        {
            string mapUrl;

            if (useGoogleMaps && !string.IsNullOrEmpty(googleMapsAPIKey))
            {
                mapUrl = BuildGoogleMapsURL(latitude, longitude, markers);
            }
            else if (!string.IsNullOrEmpty(mapboxAPIKey))
            {
                mapUrl = BuildMapboxURL(latitude, longitude, markers);
            }
            else
            {
                OnMapError?.Invoke("No valid map API key found");
                yield break;
            }

            Debug.Log($"[MapManager] Loading map from: {mapUrl}");

            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(mapUrl))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    currentMapTexture = DownloadHandlerTexture.GetContent(request);
                    OnMapImageReceived?.Invoke(currentMapTexture);
                    Debug.Log("[MapManager] Map image loaded successfully");
                }
                else
                {
                    Debug.LogError($"[MapManager] Failed to load map: {request.error}");
                    OnMapError?.Invoke($"Failed to load map: {request.error}");
                }
            }
        }

        private string BuildGoogleMapsURL(float lat, float lon, string markers = "")
        {
            string url = $"https://maps.googleapis.com/maps/api/staticmap?" +
                        $"center={lat},{lon}" +
                        $"&zoom={defaultZoom}" +
                        $"&size={mapWidth}x{mapHeight}" +
                        $"&maptype={mapType}" +
                        $"&key={googleMapsAPIKey}";

            if (!string.IsNullOrEmpty(markers))
            {
                url += $"&markers={markers}";
            }

            return url;
        }

        private string BuildMapboxURL(float lat, float lon, string markers = "")
        {
            string url = $"https://api.mapbox.com/styles/v1/mapbox/streets-v11/static/" +
                        $"{lon},{lat},{defaultZoom}/{mapWidth}x{mapHeight}" +
                        $"?access_token={mapboxAPIKey}";

            return url;
        }

        private IEnumerator GetDirections(string destination)
        {
            if (currentLocation == null)
            {
                OnMapError?.Invoke("Current location not available for navigation");
                yield break;
            }

            // First, geocode the destination
            yield return StartCoroutine(GeocodeDestination(destination));
        }

        private IEnumerator GeocodeDestination(string destination)
        {
            string geocodeUrl;

            if (useGoogleMaps && !string.IsNullOrEmpty(googleMapsAPIKey))
            {
                geocodeUrl = $"https://maps.googleapis.com/maps/api/geocode/json?" +
                           $"address={UnityWebRequest.EscapeURL(destination)}" +
                           $"&key={googleMapsAPIKey}";
            }
            else
            {
                OnMapError?.Invoke("Geocoding not available without Google Maps API");
                yield break;
            }

            using (UnityWebRequest request = UnityWebRequest.Get(geocodeUrl))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var geocodeResponse = JsonUtility.FromJson<GeocodeResponse>(request.downloadHandler.text);
                        
                        if (geocodeResponse.results != null && geocodeResponse.results.Length > 0)
                        {
                            var result = geocodeResponse.results[0];
                            var location = result.geometry.location;
                            
                            LocationData destinationLocation = new LocationData(
                                location.lat, location.lng);
                            
                            // Start navigation
                            StartNavigation(destinationLocation, result.formatted_address);
                        }
                        else
                        {
                            OnMapError?.Invoke($"Could not find location: {destination}");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[MapManager] Geocoding error: {e.Message}");
                        OnMapError?.Invoke("Failed to process location data");
                    }
                }
                else
                {
                    OnMapError?.Invoke($"Geocoding failed: {request.error}");
                }
            }
        }

        private void StartNavigation(LocationData destination, string address)
        {
            currentNavigation = new NavigationData(destination, address);
            
            // Calculate distance and bearing
            currentNavigation.distance = CalculateDistance(currentLocation, destination);
            currentNavigation.bearing = CalculateBearing(currentLocation, destination);
            
            isNavigating = true;
            
            // Create navigation arrow
            CreateNavigationArrow();
            
            // Load map with route
            StartCoroutine(LoadNavigationMap());
            
            OnNavigationDataReceived?.Invoke(currentNavigation);
            Debug.Log($"[MapManager] Navigation started to: {address}");
        }

        private IEnumerator LoadNavigationMap()
        {
            string markers = $"color:red|{currentLocation.latitude},{currentLocation.longitude}|" +
                           $"color:green|{currentNavigation.destination.latitude},{currentNavigation.destination.longitude}";
            
            yield return StartCoroutine(LoadStaticMap(
                (currentLocation.latitude + currentNavigation.destination.latitude) / 2,
                (currentLocation.longitude + currentNavigation.destination.longitude) / 2,
                markers));
        }

        private void CreateNavigationArrow()
        {
            if (navigationArrowPrefab == null) return;

            // Remove existing arrow
            if (currentNavigationArrow != null)
            {
                DestroyImmediate(currentNavigationArrow);
            }

            // Create new arrow
            Vector3 arrowPosition = Camera.main.transform.position + 
                                  Camera.main.transform.forward * arrowDistance +
                                  Vector3.up * arrowHeight;

            currentNavigationArrow = Instantiate(navigationArrowPrefab, arrowPosition, Quaternion.identity);
            
            // Point arrow towards destination
            UpdateNavigationArrow();
        }

        private void UpdateNavigationArrow()
        {
            if (currentNavigationArrow == null || currentNavigation == null) return;

            // Calculate direction to destination
            float bearing = currentNavigation.bearing;
            
            // Convert bearing to Unity rotation
            Quaternion rotation = Quaternion.Euler(0, bearing, 0);
            currentNavigationArrow.transform.rotation = rotation;
        }

        private float CalculateDistance(LocationData from, LocationData to)
        {
            // Haversine formula for distance calculation
            const float R = 6371000; // Earth's radius in meters
            
            float lat1Rad = from.latitude * Mathf.Deg2Rad;
            float lat2Rad = to.latitude * Mathf.Deg2Rad;
            float deltaLatRad = (to.latitude - from.latitude) * Mathf.Deg2Rad;
            float deltaLonRad = (to.longitude - from.longitude) * Mathf.Deg2Rad;

            float a = Mathf.Sin(deltaLatRad / 2) * Mathf.Sin(deltaLatRad / 2) +
                     Mathf.Cos(lat1Rad) * Mathf.Cos(lat2Rad) *
                     Mathf.Sin(deltaLonRad / 2) * Mathf.Sin(deltaLonRad / 2);
            
            float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
            
            return R * c;
        }

        private float CalculateBearing(LocationData from, LocationData to)
        {
            float lat1Rad = from.latitude * Mathf.Deg2Rad;
            float lat2Rad = to.latitude * Mathf.Deg2Rad;
            float deltaLonRad = (to.longitude - from.longitude) * Mathf.Deg2Rad;

            float y = Mathf.Sin(deltaLonRad) * Mathf.Cos(lat2Rad);
            float x = Mathf.Cos(lat1Rad) * Mathf.Sin(lat2Rad) -
                     Mathf.Sin(lat1Rad) * Mathf.Cos(lat2Rad) * Mathf.Cos(deltaLonRad);

            float bearing = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
            return (bearing + 360) % 360; // Normalize to 0-360
        }

        public void StopNavigation()
        {
            isNavigating = false;
            currentNavigation = null;
            
            if (currentNavigationArrow != null)
            {
                DestroyImmediate(currentNavigationArrow);
                currentNavigationArrow = null;
            }
            
            Debug.Log("[MapManager] Navigation stopped");
        }

        private void Update()
        {
            // Update navigation arrow if navigating
            if (isNavigating && currentNavigation != null)
            {
                UpdateNavigationArrow();
            }
        }

        public void SetMapRenderer(Renderer renderer)
        {
            mapRenderer = renderer;
            if (currentMapTexture != null && mapRenderer != null)
            {
                mapRenderer.material.mainTexture = currentMapTexture;
            }
        }

        // Public properties
        public bool IsLocationServiceRunning => isLocationServiceRunning;
        public bool IsNavigating => isNavigating;
        public LocationData CurrentLocation => currentLocation;
        public NavigationData CurrentNavigation => currentNavigation;

        private void OnDestroy()
        {
            if (isLocationServiceRunning)
            {
                Input.location.Stop();
            }
        }

        // Data classes for JSON parsing
        [System.Serializable]
        private class MapConfig
        {
            public string google_maps_api_key;
            public string mapbox_api_key;
        }

        [System.Serializable]
        private class GeocodeResponse
        {
            public GeocodeResult[] results;
            public string status;
        }

        [System.Serializable]
        private class GeocodeResult
        {
            public string formatted_address;
            public GeocodeGeometry geometry;
        }

        [System.Serializable]
        private class GeocodeGeometry
        {
            public GeocodeLocation location;
        }

        [System.Serializable]
        private class GeocodeLocation
        {
            public float lat;
            public float lng;
        }
    }
}