using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using iconnect;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace AudioLibraryExtension
{
    public class AudioLibrary : IExtension
    {
        private IHostApp Host { get; set; }
        private string configPath = "audios_config.json";
        private AudioConfig config;
        private Dictionary<string, string> audioCache = new Dictionary<string, string>();
        private Dictionary<string, bool> downloadInProgress = new Dictionary<string, bool>();
        private object cacheLock = new object();

        public AudioLibrary(IHostApp host)
        {
            this.Host = host;
            LoadConfig();
            Console.WriteLine($"🎵 Audio Library Extension loaded. {config.Audios.Count} audios in list.");
        }

        public void ServerStarted()
        {
            Console.WriteLine("🎵 Audio Library Extension: Server started");
        }

        private bool IsAudioCached(string url)
        {
            lock (cacheLock)
            {
                return audioCache.ContainsKey(url);
            }
        }

        private int GetCacheSizeKB()
        {
            lock (cacheLock)
            {
                int totalSize = 0;
                foreach (var kvp in audioCache)
                {
                    totalSize += kvp.Value.Length;
                }
                return totalSize / 1024;
            }
        }

        private void CacheAudioInBackground(AudioEntry audio)
        {
            if (IsAudioCached(audio.Url) || downloadInProgress.ContainsKey(audio.Url))
                return;

            try
            {
                downloadInProgress[audio.Url] = true;
                string base64 = DownloadAndConvertToBase64(audio.Url);

                if (!string.IsNullOrEmpty(base64))
                {
                    lock (cacheLock)
                    {
                        audioCache[audio.Url] = base64;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error cargando audio '{audio.Name}': {ex.Message}");
            }
            finally
            {
                downloadInProgress.Remove(audio.Url);
            }
        }

        private string DownloadAndConvertToBase64(string url)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                    client.Proxy = null;

                    byte[] audioData = client.DownloadData(url);
                    string mimeType = GetMimeType(url);
                    string base64 = Convert.ToBase64String(audioData);
                    return $"data:{mimeType};base64,{base64}";
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error descargando audio: {ex.Message}");
            }
        }

        private string GetMimeType(string url)
        {
            string extension = Path.GetExtension(url).ToLower();

            if (extension == ".mp3")
                return "audio/mpeg";
            else if (extension == ".wav")
                return "audio/wav";
            else if (extension == ".ogg")
                return "audio/ogg";
            else if (extension == ".m4a" || extension == ".mp4")
                return "audio/mp4";
            else if (extension == ".aac")
                return "audio/aac";
            else if (extension == ".webm")
                return "audio/webm";
            else
                return "audio/mpeg";
        }

        public void Dispose()
        {
            lock (cacheLock)
            {
                audioCache.Clear();
            }
            downloadInProgress.Clear();
            Console.WriteLine("🎵 Audio Library Extension: Disposed");
        }

        private void LoadConfig()
        {
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    config = JsonConvert.DeserializeObject<AudioConfig>(json);
                    Console.WriteLine($"📁 Config loaded from {configPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error loading config: {ex.Message}");
                    config = new AudioConfig();
                }
            }
            else
            {
                Console.WriteLine($"📁 Config file not found, creating default...");
                config = new AudioConfig();

                config.Audios.Add(new AudioEntry
                {
                    Name = "Tienes un mensaje",
                    Url = "https://www.myinstants.com/media/sounds/tienes-un-mensajeee.mp3",
                    Owner = "System",
                    IsPublic = true,
                    AddedBy = "System",
                    AddedDate = DateTime.Now
                });

                config.Audios.Add(new AudioEntry
                {
                    Name = "Sonido especial",
                    Url = "https://www.myinstants.com/media/sounds/y2mate_1lLaYg7.mp3",
                    Owner = "System",
                    IsPublic = true,
                    AddedBy = "System",
                    AddedDate = DateTime.Now
                });

                config.Audios.Add(new AudioEntry
                {
                    Name = "Homero gimiendo",
                    Url = "https://www.myinstants.com/media/sounds/homero-gimiendo.mp3",
                    Owner = "System",
                    IsPublic = true,
                    AddedBy = "System",
                    AddedDate = DateTime.Now
                });

                config.Audios.Add(new AudioEntry
                {
                    Name = "Dexter Meme",
                    Url = "https://www.myinstants.com/media/sounds/dexter-meme.mp3",
                    Owner = "System",
                    IsPublic = true,
                    AddedBy = "System",
                    AddedDate = DateTime.Now
                });

                SaveConfig();
                Console.WriteLine($"✅ Created default config with {config.Audios.Count} audios");
            }
        }

        private void SaveConfig()
        {
            try
            {
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error saving config: {ex.Message}");
            }
        }

        public void Command(IUser client, string cmd, IUser target, string args)
        {
            Console.WriteLine($"🎵 Command received: /{cmd} from {client.Name}");

            try
            {
                string command = cmd.ToLower();

                // Handle /play <url> command
                if (command.StartsWith("play "))
                {
                    string url = cmd.Substring(5).Trim();
                    HandlePlayFromUrl(client, url);
                    return;
                }

                // Handle /audio <id> or just /audio
                if (command.StartsWith("audio"))
                {
                    if (cmd.Length > 5)
                    {
                        if (cmd[5] == ' ')
                        {
                            string idStr = cmd.Substring(6).Trim();
                            HandlePlayAudioById(client, idStr);
                        }
                        else
                        {
                            HandleListAudios(client);
                        }
                    }
                    else
                    {
                        HandleListAudios(client);
                    }
                    return;
                }

                // Handle /audios command
                if (command == "audios")
                {
                    HandleListAudios(client);
                    return;
                }

                // Handle /pmaudio <id> <username>
                if (command.StartsWith("pmaudio "))
                {
                    string parameters = cmd.Substring(8).Trim();
                    HandlePmAudioSubstring(client, parameters);
                    return;
                }

                // Handle /addaudio <url> <name>
                if (command.StartsWith("addaudio "))
                {
                    string parameters = cmd.Substring(9).Trim();
                    HandleAddAudioSubstring(client, parameters);
                    return;
                }

                // Handle /removeaudio <id>
                if (command.StartsWith("removeaudio "))
                {
                    string idStr = cmd.Substring(12).Trim();
                    HandleRemoveAudioSubstring(client, idStr);
                    return;
                }

                // Handle simple commands without parameters
                if (command == "audiocache")
                {
                    HandleCacheInfo(client);
                    return;
                }
                else if (command == "precache")
                {
                    HandlePreCache(client);
                    return;
                }
                else if (command == "debug")
                {
                    HandleDebug(client);
                    return;
                }
                else if (command == "testlibaudio")
                {
                    HandleTestLibraryAudio(client);
                    return;
                }
                else if (command == "help")
                {
                    Help(client);
                    return;
                }
                else if (command == "clearcache")
                {
                    ClearCache();
                    client.Print("✅ Cache limpiado.");
                    return;
                }

                client.Print("❌ Comando no reconocido. Usa /help para ver los comandos disponibles.");
            }
            catch (Exception ex)
            {
                client.Print($"❌ Error: {ex.Message}");
            }
        }

        private void HandlePlayFromUrl(IUser client, string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url))
                {
                    client.Print("❌ URL requerida. Uso: /play <url>");
                    return;
                }

                client.Print("🔄 Procesando URL...");

                string audioUrl = null;
                
                // FIRST: Check if it's a direct audio URL
                if (IsValidAudioUrl(url))
                {
                    audioUrl = url;
                    client.Print($"✅ URL es un archivo de audio directo.");
                }
                // SECOND: Check if it's a MyInstants page and extract audio URL
                else if (url.Contains("myinstants.com"))
                {
                    audioUrl = ExtractAudioUrlFromMyInstants(url);
                    if (audioUrl != null)
                    {
                        client.Print($"✅ Extraído audio de página MyInstants: {audioUrl}");
                    }
                    else
                    {
                        client.Print("❌ No se pudo extraer un enlace de audio de la página MyInstants.");
                        return;
                    }
                }
                else
                {
                    client.Print("❌ URL no válida. Debe ser un enlace directo a audio (.mp3, .wav, etc.) o una página de MyInstants.");
                    return;
                }

                client.Print($"🔄 Descargando audio desde: {audioUrl}");

                string base64Audio = DownloadAndConvertToBase64(audioUrl);

                if (!string.IsNullOrEmpty(base64Audio))
                {
                    lock (cacheLock)
                    {
                        audioCache[audioUrl] = base64Audio;
                    }

                    SendAudioToAllBase64(client.Name, base64Audio);
                    client.Print("✅ Audio desde URL enviado a la sala.");
                }
                else
                {
                    client.Print("❌ No se pudo cargar el audio desde la URL.");
                }
            }
            catch (Exception ex)
            {
                client.Print($"❌ Error: {ex.Message}");
            }
        }

        private string ExtractAudioUrlFromMyInstants(string url)
        {
            // Check if it's a MyInstants URL
            if (!url.Contains("myinstants.com"))
                return null;

            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                    client.Proxy = null;
                    client.Encoding = System.Text.Encoding.UTF8;

                    Console.WriteLine($"🔄 Extrayendo audio de MyInstants: {url}");
                    string html = client.DownloadString(url);

                    // Method 1: Look for the preloadAudioUrl variable in JavaScript (most reliable)
                    var preloadRegex = new Regex(@"preloadAudioUrl\s*=\s*['""]([^'""]+)['""]");
                    var preloadMatch = preloadRegex.Match(html);

                    if (preloadMatch.Success)
                    {
                        string audioPath = preloadMatch.Groups[1].Value.Trim();
                        Console.WriteLine($"✅ Encontrado preloadAudioUrl: {audioPath}");
                        
                        // If it's a relative path, make it absolute
                        if (audioPath.StartsWith("/"))
                        {
                            if (audioPath.StartsWith("//"))
                            {
                                return "https:" + audioPath;
                            }
                            return "https://www.myinstants.com" + audioPath;
                        }
                        else if (audioPath.StartsWith("media/"))
                        {
                            return "https://www.myinstants.com/" + audioPath;
                        }
                        
                        return audioPath;
                    }

                    // Method 2: Look for audio elements
                    var audioRegex = new Regex(@"<audio[^>]+src=['""]([^'""]+)['""]");
                    var audioMatch = audioRegex.Match(html);
                    
                    if (audioMatch.Success)
                    {
                        string audioSrc = audioMatch.Groups[1].Value.Trim();
                        Console.WriteLine($"✅ Encontrado elemento audio con src: {audioSrc}");
                        return MakeAbsoluteUrl(audioSrc, url);
                    }

                    // Method 3: Look for source elements inside audio
                    var sourceRegex = new Regex(@"<source[^>]+src=['""]([^'""]+)['""][^>]+type=['""]audio/");
                    var sourceMatch = sourceRegex.Match(html);
                    
                    if (sourceMatch.Success)
                    {
                        string audioSrc = sourceMatch.Groups[1].Value.Trim();
                        Console.WriteLine($"✅ Encontrado elemento source con src: {audioSrc}");
                        return MakeAbsoluteUrl(audioSrc, url);
                    }

                    // Method 4: Look for MP3 links in the page
                    var mp3Regex = new Regex(@"href=['""]([^'""]*\.mp3)['""]");
                    var mp3Matches = mp3Regex.Matches(html);
                    
                    foreach (Match match in mp3Matches)
                    {
                        string mp3Url = match.Groups[1].Value.Trim();
                        if (mp3Url.Contains("media/sounds/"))
                        {
                            Console.WriteLine($"✅ Encontrado enlace MP3: {mp3Url}");
                            return MakeAbsoluteUrl(mp3Url, url);
                        }
                    }

                    // Method 5: Look for any .mp3 file
                    if (mp3Matches.Count > 0)
                    {
                        string mp3Url = mp3Matches[0].Groups[1].Value.Trim();
                        Console.WriteLine($"✅ Encontrado enlace MP3: {mp3Url}");
                        return MakeAbsoluteUrl(mp3Url, url);
                    }

                    Console.WriteLine($"⚠️ No se encontró audio en la página MyInstants: {url}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error extrayendo URL de audio de MyInstants: {ex.Message}");
                return null;
            }
        }

        private string MakeAbsoluteUrl(string relativeUrl, string baseUrl)
        {
            if (string.IsNullOrEmpty(relativeUrl))
                return null;

            if (relativeUrl.StartsWith("http://") || relativeUrl.StartsWith("https://"))
                return relativeUrl;

            if (relativeUrl.StartsWith("//"))
                return "https:" + relativeUrl;

            if (relativeUrl.StartsWith("/"))
            {
                // Get the domain from baseUrl
                Uri baseUri;
                try
                {
                    baseUri = new Uri(baseUrl);
                }
                catch
                {
                    baseUri = new Uri("https://www.myinstants.com");
                }
                return $"{baseUri.Scheme}://{baseUri.Host}{relativeUrl}";
            }

            // Relative path without leading slash
            Uri uri = new Uri(baseUrl);
            string basePath = uri.AbsoluteUri.Substring(0, uri.AbsoluteUri.LastIndexOf('/') + 1);
            return basePath + relativeUrl;
        }

        private bool IsValidAudioUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            // Check for direct audio file extensions
            string[] audioExtensions = { ".mp3", ".wav", ".ogg", ".m4a", ".mp4", ".aac", ".webm", ".flac" };
            string extension = Path.GetExtension(url).ToLower();
            
            if (audioExtensions.Contains(extension))
                return true;

            // Check if it looks like a MyInstants sound file
            if (url.Contains("media/sounds/"))
                return true;

            // Check if it's a data URL (base64 encoded audio)
            if (url.StartsWith("data:audio/"))
                return true;

            // Check for common audio file patterns in URLs
            if (url.Contains(".mp3?") || url.Contains(".wav?") || url.Contains(".ogg?"))
                return true;

            return false;
        }

        private void HandlePlayAudioById(IUser client, string audioIdStr)
        {
            try
            {
                if (config.Audios.Count == 0)
                {
                    client.Print("❌ No hay audios disponibles.");
                    return;
                }

                if (!int.TryParse(audioIdStr, out int userFriendlyId) || userFriendlyId < 1 || userFriendlyId > config.Audios.Count)
                {
                    client.Print($"❌ ID inválido. Usa /audios para ver la lista (IDs válidos: 1-{config.Audios.Count}).");
                    return;
                }

                int actualId = userFriendlyId - 1;
                var audio = config.Audios[actualId];

                if (!IsAudioCached(audio.Url))
                {
                    string base64Audio = DownloadAndConvertToBase64(audio.Url);
                    if (!string.IsNullOrEmpty(base64Audio))
                    {
                        lock (cacheLock)
                        {
                            audioCache[audio.Url] = base64Audio;
                        }
                    }
                }

                string cachedAudio;
                lock (cacheLock)
                {
                    cachedAudio = audioCache[audio.Url];
                }

                if (!string.IsNullOrEmpty(cachedAudio))
                {
                    SendAudioToAllBase64(client.Name, cachedAudio);
                    client.Print($"✅ Audio '{audio.Name}' enviado a la sala.");
                }
                else
                {
                    client.Print("❌ Error al cargar el audio.");
                }
            }
            catch (Exception ex)
            {
                client.Print($"❌ Error: {ex.Message}");
            }
        }

        private void HandlePmAudioSubstring(IUser client, string parameters)
        {
            if (string.IsNullOrWhiteSpace(parameters))
            {
                client.Print("❌ Uso: /pmaudio <id> <usuario>");
                return;
            }

            string[] parts = parameters.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2 || !int.TryParse(parts[0], out int id) || id < 1 || id > config.Audios.Count)
            {
                client.Print($"❌ ID inválido. Uso: /pmaudio <id> <usuario> (IDs: 1-{config.Audios.Count})");
                return;
            }

            var audio = config.Audios[id - 1];
            string targetUsername = parts[1];

            if (!IsAudioCached(audio.Url))
            {
                client.Print($"🔄 Descargando '{audio.Name}'...");
                DownloadAndSendPmAudio(client, targetUsername, audio);
            }
            else
            {
                try
                {
                    SendPmAudioToUser(client, targetUsername, audio);
                }
                catch (Exception ex)
                {
                    client.Print($"❌ Error: {ex.Message}");
                }
            }
        }

        private void HandleAddAudioSubstring(IUser client, string parameters)
        {
            if (string.IsNullOrWhiteSpace(parameters))
            {
                client.Print("❌ Uso: /addaudio <url> <nombre>");
                return;
            }

            string[] parts = parameters.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
            {
                client.Print("❌ Formato incorrecto. Uso: /addaudio <url> <nombre>");
                return;
            }

            string url = parts[0].Trim();
            string name = parts[1].Trim();

            client.Print($"🔄 Procesando URL: {url}");

            string audioUrl = null;
            
            // FIRST: Check if it's a direct audio URL
            if (IsValidAudioUrl(url))
            {
                audioUrl = url;
                client.Print($"✅ URL es un archivo de audio directo.");
            }
            // SECOND: Check if it's a MyInstants page and extract audio URL
            else if (url.Contains("myinstants.com"))
            {
                audioUrl = ExtractAudioUrlFromMyInstants(url);
                if (audioUrl != null)
                {
                    client.Print($"✅ Extraído audio de página MyInstants: {audioUrl}");
                }
                else
                {
                    client.Print("❌ No se pudo extraer un enlace de audio de la página MyInstants.");
                    return;
                }
            }
            else
            {
                client.Print("❌ URL no válida. Debe ser un enlace directo a audio (.mp3, .wav, etc.) o una página de MyInstants.");
                return;
            }

            // Check for duplicate name
            if (config.Audios.Any(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                client.Print("❌ Ya existe un audio con ese nombre. Usa un nombre diferente.");
                return;
            }

            // Check for duplicate URL
            if (config.Audios.Any(a => a.Url.Equals(audioUrl, StringComparison.OrdinalIgnoreCase)))
            {
                client.Print("❌ Ya existe un audio con esa URL.");
                return;
            }

            // Test if we can download the audio
            client.Print($"🔄 Probando descarga del audio...");
            try
            {
                using (WebClient testClient = new WebClient())
                {
                    testClient.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                    testClient.Proxy = null;
                    
                    // Try to download just a small portion first
                    testClient.DownloadData(audioUrl);
                }
            }
            catch (Exception ex)
            {
                client.Print($"❌ No se pudo acceder al audio: {ex.Message}");
                return;
            }

            config.Audios.Add(new AudioEntry
            {
                Name = name,
                Url = audioUrl,
                Owner = client.Name,
                IsPublic = true,
                AddedBy = client.Name,
                AddedDate = DateTime.Now
            });

            SaveConfig();
            
            // Try to pre-cache the audio
            Task.Run(() =>
            {
                try
                {
                    CacheAudioInBackground(config.Audios.Last());
                    Console.WriteLine($"✅ Audio '{name}' pre-cached.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Audio '{name}' added but could not be pre-cached: {ex.Message}");
                }
            });

            client.Print($"✅ Audio '{name}' agregado exitosamente. URL: {audioUrl}");
        }

        private void HandleRemoveAudioSubstring(IUser client, string idStr)
        {
            if (string.IsNullOrWhiteSpace(idStr) || !int.TryParse(idStr, out int id) || id < 1 || id > config.Audios.Count)
            {
                client.Print($"❌ ID inválido. Usa /audios para ver IDs válidos (1-{config.Audios.Count}).");
                return;
            }

            int actualId = id - 1;
            var audio = config.Audios[actualId];

            if (audio.Owner != client.Name && client.Level < ILevel.Moderator)
            {
                client.Print("❌ Solo el dueño puede eliminar este audio.");
                return;
            }

            // Remove from cache
            lock (cacheLock)
            {
                if (audioCache.ContainsKey(audio.Url))
                {
                    audioCache.Remove(audio.Url);
                }
            }

            config.Audios.RemoveAt(actualId);
            SaveConfig();
            client.Print($"✅ Audio '{audio.Name}' eliminado.");
        }

        private void HandleListAudios(IUser client)
        {
            if (config.Audios.Count == 0)
            {
                client.Print("📭 No hay audios disponibles.");
                return;
            }

            client.Print("🎵 Lista de audios disponibles:");
            for (int i = 0; i < config.Audios.Count; i++)
            {
                var audio = config.Audios[i];
                string status = IsAudioCached(audio.Url) ? "✅" : "⬇️";
                string ownerBadge = audio.Owner == client.Name ? "👑 " : "";
                client.Print($"{i + 1}. {status} {ownerBadge}{audio.Name} - Dueño: {audio.Owner}");
            }
        }

        private void DownloadAndSendPmAudio(IUser client, string targetIdentifier, AudioEntry audio)
        {
            try
            {
                string base64Audio = DownloadAndConvertToBase64(audio.Url);

                if (!string.IsNullOrEmpty(base64Audio))
                {
                    lock (cacheLock)
                    {
                        audioCache[audio.Url] = base64Audio;
                    }

                    SendPmAudioToUserBase64(client, targetIdentifier, audio, base64Audio);
                }
            }
            catch (Exception ex)
            {
                client.Print($"❌ Error descargando '{audio.Name}': {ex.Message}");
            }
        }

        private void SendPmAudioToUser(IUser sender, string targetIdentifier, AudioEntry audio)
        {
            try
            {
                string base64Audio;
                lock (cacheLock)
                {
                    if (!audioCache.TryGetValue(audio.Url, out base64Audio))
                        throw new Exception("Audio no encontrado en caché.");
                }

                SendPmAudioToUserBase64(sender, targetIdentifier, audio, base64Audio);
            }
            catch (Exception ex)
            {
                sender.Print($"❌ Error: {ex.Message}");
            }
        }

        private void HandleCacheInfo(IUser client)
        {
            lock (cacheLock)
            {
                int cachedCount = audioCache.Count;
                int totalSize = GetCacheSizeKB();
                client.Print($"📊 Cache: {cachedCount} audios, {totalSize} KB");
            }
        }

        private void HandlePreCache(IUser client)
        {
            client.Print("🔄 Precargando todos los audios...");

            Task.Run(() =>
            {
                try
                {
                    int cachedCount = 0;
                    foreach (var audio in config.Audios)
                    {
                        if (!IsAudioCached(audio.Url))
                        {
                            CacheAudioInBackground(audio);
                            cachedCount++;
                        }
                        Thread.Sleep(200);
                    }
                    client.Print($"✅ Precarga completada. {cachedCount} audios cargados en caché.");
                }
                catch (Exception ex)
                {
                    client.Print($"❌ Error en precarga: {ex.Message}");
                }
            });
        }

        private void HandleDebug(IUser client)
        {
            client.Print("=== 🐛 Debug Information ===");
            client.Print($"• Extension loaded: {config != null}");
            client.Print($"• Audio count: {config?.Audios.Count ?? 0}");
            client.Print($"• Cache size: {GetCacheSizeKB()} KB");
            client.Print($"• Cached audios: {audioCache.Count}");
        }

        private void HandleTestLibraryAudio(IUser client)
        {
            try
            {
                if (config.Audios.Count == 0)
                {
                    client.Print("❌ No hay audios en la biblioteca.");
                    return;
                }

                var audio = config.Audios[0];
                client.Print($"🎵 Probando audio: {audio.Name}");

                if (!IsAudioCached(audio.Url))
                {
                    client.Print("🔄 Descargando para prueba...");
                    string base64 = DownloadAndConvertToBase64(audio.Url);

                    if (!string.IsNullOrEmpty(base64))
                    {
                        SendAudioToAllBase64(client.Name, base64);
                        client.Print($"✅ Audio de prueba '{audio.Name}' enviado.");
                    }
                    else
                    {
                        client.Print("❌ No se pudo descargar el audio.");
                    }
                }
                else
                {
                    string base64;
                    lock (cacheLock)
                    {
                        base64 = audioCache[audio.Url];
                    }
                    SendAudioToAllBase64(client.Name, base64);
                    client.Print($"✅ Audio de prueba '{audio.Name}' enviado desde caché.");
                }
            }
            catch (Exception ex)
            {
                client.Print($"❌ Error en prueba: {ex.Message}");
            }
        }

        private void SendAudioToAllBase64(string senderName, string base64Audio)
        {
            try
            {
                var coreAssembly = Assembly.Load("core");
                var userPoolType = coreAssembly.GetType("core.UserPool");
                var wUsersField = userPoolType.GetField("WUsers", BindingFlags.Public | BindingFlags.Static);
                var wUsers = wUsersField.GetValue(null) as System.Collections.IEnumerable;

                ushort senderVroom = 0;
                foreach (var user in wUsers)
                {
                    var userType = user.GetType();
                    var name = (string)userType.GetProperty("Name").GetValue(user);
                    var loggedIn = (bool)userType.GetProperty("LoggedIn").GetValue(user);

                    if (loggedIn && name == senderName)
                    {
                        senderVroom = (ushort)userType.GetProperty("Vroom").GetValue(user);
                        break;
                    }
                }

                int sentCount = 0;
                foreach (var user in wUsers)
                {
                    try
                    {
                        var userType = user.GetType();
                        var loggedIn = (bool)userType.GetProperty("LoggedIn").GetValue(user);
                        var vroom = (ushort)userType.GetProperty("Vroom").GetValue(user);
                        var quarantined = (bool)userType.GetProperty("Quarantined").GetValue(user);
                        var extended = (bool)userType.GetProperty("Extended").GetValue(user);
                        var isInbizierWeb = (bool)userType.GetProperty("IsInbizierWeb").GetValue(user);
                        var isInbizierMobile = (bool)userType.GetProperty("IsInbizierMobile").GetValue(user);

                        if (loggedIn && vroom == senderVroom && !quarantined && extended && (isInbizierWeb || isInbizierMobile))
                        {
                            var audioMethod = userType.GetMethod("Audio", new[] { typeof(string), typeof(string) });
                            audioMethod?.Invoke(user, new object[] { senderName, base64Audio });
                            sentCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending audio to user: {ex.Message}");
                    }
                }

                if (sentCount == 0)
                {
                    throw new Exception("No se encontraron usuarios compatibles para enviar el audio.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error enviando audio: {ex.Message}");
            }
        }

        private void SendPmAudioToUserBase64(IUser sender, string targetIdentifier, AudioEntry audio, string base64Audio)
        {
            try
            {
                var coreAssembly = Assembly.Load("core");
                var userPoolType = coreAssembly.GetType("core.UserPool");
                var wUsersField = userPoolType.GetField("WUsers", BindingFlags.Public | BindingFlags.Static);
                var wUsers = wUsersField.GetValue(null) as System.Collections.IEnumerable;

                object targetUser = null;

                if (ushort.TryParse(targetIdentifier, out ushort userId))
                {
                    foreach (var user in wUsers)
                    {
                        var userType = user.GetType();
                        var id = (ushort)userType.GetProperty("ID").GetValue(user);
                        var loggedIn = (bool)userType.GetProperty("LoggedIn").GetValue(user);

                        if (loggedIn && id == userId)
                        {
                            targetUser = user;
                            break;
                        }
                    }
                }
                else
                {
                    foreach (var user in wUsers)
                    {
                        var userType = user.GetType();
                        var name = (string)userType.GetProperty("Name").GetValue(user);
                        var loggedIn = (bool)userType.GetProperty("LoggedIn").GetValue(user);

                        if (loggedIn && name.Equals(targetIdentifier, StringComparison.OrdinalIgnoreCase))
                        {
                            targetUser = user;
                            break;
                        }
                    }
                }

                if (targetUser == null)
                {
                    sender.Print($"❌ Usuario '{targetIdentifier}' no encontrado.");
                    return;
                }

                var targetType = targetUser.GetType();
                var targetName = (string)targetType.GetProperty("Name").GetValue(targetUser);
                var isInbizierWeb = (bool)targetType.GetProperty("IsInbizierWeb").GetValue(targetUser);
                var isInbizierMobile = (bool)targetType.GetProperty("IsInbizierMobile").GetValue(targetUser);

                if (!isInbizierWeb && !isInbizierMobile)
                {
                    sender.Print($"❌ {targetName} no puede recibir audios.");
                    return;
                }

                var ignoreList = targetType.GetProperty("IgnoreList").GetValue(targetUser) as System.Collections.IList;
                if (ignoreList != null && ignoreList.Contains(sender.Name))
                {
                    sender.Print($"❌ {targetName} te está ignorando.");
                    return;
                }

                var pmAudioMethod = targetType.GetMethod("PmAudio", new[] { typeof(string), typeof(string) });
                pmAudioMethod?.Invoke(targetUser, new object[] { sender.Name, base64Audio });

                sender.Print($"✅ Audio '{audio.Name}' enviado en privado a {targetName}.");
            }
            catch (Exception ex)
            {
                sender.Print($"❌ Error: {ex.Message}");
            }
        }

        private void ClearCache()
        {
            lock (cacheLock)
            {
                audioCache.Clear();
            }
            downloadInProgress.Clear();
        }

        #region Helper Classes
        private class AudioConfig
        {
            public List<AudioEntry> Audios { get; set; } = new List<AudioEntry>();
        }

        private class AudioEntry
        {
            public string Name { get; set; }
            public string Url { get; set; }
            public string Owner { get; set; }
            public bool IsPublic { get; set; } = true;
            public string AddedBy { get; set; }
            public DateTime AddedDate { get; set; }
        }
        #endregion

        #region IExtension Implementation
        public void Load() { }
        public void CycleTick() { }
        public void UnhandledProtocol(IUser client, bool custom, byte msg, byte[] packet) { }
        public bool Joining(IUser client) => true;
        public void Joined(IUser client) { }
        public void Rejected(IUser client, RejectedMsg msg) { }
        public void Parting(IUser client) { }
        public void Parted(IUser client) { }
        public bool AvatarReceived(IUser client) => true;
        public bool PersonalMessageReceived(IUser client, string text) => true;
        public void TextReceived(IUser client, string text) { }
        public string TextSending(IUser client, string text) => text;
        public void TextSent(IUser client, string text) { }
        public void EmoteReceived(IUser client, string text) { }
        public string EmoteSending(IUser client, string text) => text;
        public void EmoteSent(IUser client, string text) { }
        public void PrivateSending(IUser client, IUser target, IPrivateMsg msg) { }
        public void PrivateSent(IUser client, IUser target) { }
        public void BotPrivateSent(IUser client, string text) { }
        public bool Nick(IUser client, string name) => true;

        public void Help(IUser client)
        {
            client.Print("=== 🎵 Audio Library Extension ===");
            client.Print("/audios - Muestra la lista de audios disponibles");
            client.Print("/audio - Muestra la lista de audios (atajo)");
            client.Print("/audio <id> - Reproduce un audio en la sala");
            client.Print("/play <url> - Reproduce un audio directamente desde una URL");
            client.Print("/pmaudio <id> <usuario> - Envía audio en privado");
            client.Print("/addaudio <url> <nombre> - Añade audio desde URL o MyInstants");
            client.Print("/removeaudio <id> - Elimina audio (solo dueño)");
            client.Print("/audiocache - Info del caché");
            client.Print("/precache - Precarga todos los audios");
            client.Print("/clearcache - Limpia la cache");
            client.Print("/testlibaudio - Prueba el primer audio");
            client.Print("/debug - Información de depuración");
            client.Print("/help - Muestra esta ayuda");
        }

        public void FileReceived(IUser client, string filename, string title, MimeType type) { }
        public bool Ignoring(IUser client, IUser target) => false;
        public void IgnoredStateChanged(IUser client, IUser target, bool ignored) { }
        public void InvalidLoginAttempt(IUser client) { }
        public void LoginGranted(IUser client) { }
        public void AdminLevelChanged(IUser client) { }
        public void InvalidRegistration(IUser client) { }
        public bool Registering(IUser client) => true;
        public void Registered(IUser client) { }
        public void Unregistered(IUser client) { }
        public void CaptchaSending(IUser client) { }
        public void CaptchaReply(IUser client, string reply) { }
        public bool VroomChanging(IUser client, ushort vroom) => true;
        public void VroomChanged(IUser client) { }
        public bool Flooding(IUser client, byte msg) => false;
        public void Flooded(IUser client) { }
        public bool ProxyDetected(IUser client) => false;
        public void Logout(IUser client) { }
        public void Idled(IUser client) { }
        public void Unidled(IUser client, uint seconds_away) { }
        public void BansAutoCleared() { }
        public void LinkError(ILinkError error) { }
        public void Linked() { }
        public void Unlinked() { }
        public void LeafJoined(ILeaf leaf) { }
        public void LeafParted(ILeaf leaf) { }
        public void LinkedAdminDisabled(ILeaf leaf, IUser client) { }

        public BitmapSource Icon => null;
        public UserControl GUI => null;
        #endregion
    }
}
