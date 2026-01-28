<h1>🎵 Audio Library Extension for sb0t5</h1>

<p>
<strong>Audio Library Extension</strong> is a powerful sb0t5 extension that allows users to
play, manage, cache, and send audio clips directly inside chat rooms or private messages.
Audios are downloaded, converted to Base64, cached in memory, and delivered in real time
to compatible web and mobile clients.
</p>

<hr />

<h2>✨ Features</h2>
<ul>
  <li>🔊 Play audios in public chat rooms</li>
  <li>📩 Send audios via private messages</li>
  <li>🌐 Play audio directly from external URLs</li>
  <li>📚 Persistent audio library stored in JSON</li>
  <li>⚡ In-memory Base64 audio caching</li>
  <li>📊 Cache inspection and management</li>
  <li>🚀 Background audio precaching</li>
  <li>🔐 Owner-based audio removal permissions</li>
  <li>🧪 Debug and test commands</li>
</ul>

<hr />

<h2>🛠 Requirements</h2>
<ul>
  <li>sb0t5 server</li>
  <li>.NET Framework 4.7.2</li>
  <li>x86 platform target</li>
  <li>iconnect reference</li>
  <li>Newtonsoft.Json (13.0.3)</li>
</ul>

<hr />

<h2>📦 Installation</h2>
<ol>
  <li>Compile the project targeting <code>net472</code> (x86)</li>
  <li>Copy the generated <code>extension.dll</code> into your sb0t5 <code>extensions</code> folder</li>
  <li>Restart the server</li>
  <li>The file <code>audios_config.json</code> will be created automatically on first run</li>
</ol>

<hr />

<h2>📁 Configuration</h2>

<p>
The extension uses a JSON file called <code>audios_config.json</code> to store audio entries.
If the file does not exist, a default configuration with sample audios is created automatically.
</p>

<h3>Audio Entry Fields</h3>
<ul>
  <li><strong>Name</strong> – Friendly name shown in chat</li>
  <li><strong>Url</strong> – Direct link to the audio file</li>
  <li><strong>Owner</strong> – Owner of the audio</li>
  <li><strong>IsPublic</strong> – Public availability flag</li>
  <li><strong>AddedBy</strong> – User who added the audio</li>
  <li><strong>AddedDate</strong> – Date of creation</li>
</ul>

<hr />

<h2>💬 Commands</h2>

<h3>Public Commands</h3>
<ul>
  <li><code>/audios</code> – List all available audios</li>
  <li><code>/audio</code> – Shortcut to list audios</li>
  <li><code>/audio &lt;id&gt;</code> – Play an audio in the room</li>
  <li><code>/play &lt;url&gt;</code> – Play audio directly from a URL</li>
</ul>

<h3>Private Audio</h3>
<ul>
  <li><code>/pmaudio &lt;id&gt; &lt;username|id&gt;</code> – Send audio privately</li>
</ul>

<h3>Library Management</h3>
<ul>
  <li><code>/audioadd &lt;name&gt;|&lt;url&gt;|&lt;owner&gt;</code> – Add a new audio</li>
  <li><code>/audioremove &lt;id&gt;</code> – Remove an audio (owner or moderator)</li>
</ul>

<h3>Cache & Debug</h3>
<ul>
  <li><code>/audiocache</code> – Show cache stats</li>
  <li><code>/precache</code> – Preload all audios</li>
  <li><code>/clearcache</code> – Clear cached audios</li>
  <li><code>/testlibaudio</code> – Test first audio</li>
  <li><code>/debug</code> – Show debug info</li>
  <li><code>/help</code> – Show command help</li>
</ul>

<hr />

<h2>⚙️ Technical Notes</h2>
<ul>
  <li>Audios are downloaded using <code>WebClient</code> with proxy disabled</li>
  <li>Supported formats: MP3, WAV, OGG, AAC, M4A, WEBM</li>
  <li>Audio delivery uses Base64 data URIs</li>
  <li>Only Extended Web & Mobile users receive audios</li>
  <li>Reflection is used to access sb0t core user pool</li>
</ul>

<hr />

<h2>🧹 Cleanup & Lifecycle</h2>
<ul>
  <li>Cache is cleared automatically on dispose</li>
  <li>Downloads are protected against duplication</li>
  <li>Thread-safe cache access</li>
</ul>

<hr />

<h2>📜 License</h2>
<p>
This project is provided as-is for sb0t5 servers.
You are free to modify and extend it for your own server.
</p>

<hr />

<p>
<strong>Author:</strong> Pablo Santillán<br />
<strong>Target Platform:</strong> sb0t5<br />
<strong>Language:</strong> C# (.NET Framework 4.7.2)
</p>
