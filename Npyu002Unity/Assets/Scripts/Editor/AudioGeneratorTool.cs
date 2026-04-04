using UnityEngine;
using UnityEditor;
using System.IO;

namespace ActionGame.Editor
{
    /// <summary>
    /// プロシージャルに SE/BGM の AudioClip を生成し Assets/Audio に保存する。
    /// Tools/ActionGame/Generate Audio Clips から実行。
    /// </summary>
    public static class AudioGeneratorTool
    {
        const string OutputDir = "Assets/Audio";
        const int SampleRate = 44100;

        [MenuItem("Tools/ActionGame/Generate Audio Clips")]
        public static void GenerateAll()
        {
            if (!AssetDatabase.IsValidFolder(OutputDir))
                AssetDatabase.CreateFolder("Assets", "Audio");

            SaveClip(GenerateAttack(),     "SE_Attack");
            SaveClip(GenerateHit(),        "SE_Hit");
            SaveClip(GenerateEnemyDeath(), "SE_EnemyDeath");
            SaveClip(GeneratePlayerDeath(),"SE_PlayerDeath");
            SaveClip(GenerateBGM(),        "BGM_Battle");

            AssetDatabase.Refresh();
            WireAudioManager();

            Debug.Log("[AudioGenerator] 5 clips generated and wired to AudioManager.");
        }

        // ---- クリップ生成 ----

        // 攻撃: 短い高音 click
        static AudioClip GenerateAttack()
        {
            int len = (int)(SampleRate * 0.12f);
            var data = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 25f);
                data[i] = env * Mathf.Sin(2 * Mathf.PI * 600f * t);
            }
            return Build("SE_Attack", data);
        }

        // ヒット: 低めの短音
        static AudioClip GenerateHit()
        {
            int len = (int)(SampleRate * 0.15f);
            var data = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 20f);
                float freq = 300f - t * 800f;
                data[i] = env * Mathf.Sin(2 * Mathf.PI * Mathf.Max(freq, 80f) * t);
            }
            return Build("SE_Hit", data);
        }

        // 敵撃破: 下降音 + ノイズ
        static AudioClip GenerateEnemyDeath()
        {
            int len = (int)(SampleRate * 0.5f);
            var data = new float[len];
            var rng = new System.Random(42);
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 6f);
                float freq = 400f * Mathf.Exp(-t * 5f);
                float noise = (float)(rng.NextDouble() * 2 - 1) * 0.3f;
                data[i] = env * (Mathf.Sin(2 * Mathf.PI * freq * t) + noise);
            }
            return Build("SE_EnemyDeath", data);
        }

        // プレイヤー死: 重い低音
        static AudioClip GeneratePlayerDeath()
        {
            int len = (int)(SampleRate * 0.8f);
            var data = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 4f);
                data[i] = env * (
                    Mathf.Sin(2 * Mathf.PI * 120f * t) * 0.6f +
                    Mathf.Sin(2 * Mathf.PI * 80f  * t) * 0.4f);
            }
            return Build("SE_PlayerDeath", data);
        }

        // BGM: シンプルなループ音
        static AudioClip GenerateBGM()
        {
            float duration = 4f;
            int len = (int)(SampleRate * duration);
            var data = new float[len];
            float[] freqs = { 261.6f, 329.6f, 392f, 329.6f }; // C E G E
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SampleRate;
                int note = (int)(t / (duration / freqs.Length)) % freqs.Length;
                float env = 0.3f + 0.1f * Mathf.Sin(2 * Mathf.PI * 4f * t);
                data[i] = env * (
                    Mathf.Sin(2 * Mathf.PI * freqs[note] * t) * 0.5f +
                    Mathf.Sin(2 * Mathf.PI * freqs[note] * 2 * t) * 0.25f);
            }
            return Build("BGM_Battle", data);
        }

        // ---- ユーティリティ ----

        static AudioClip Build(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static void SaveClip(AudioClip clip, string assetName)
        {
            // WAV バイト列に変換して保存
            var bytes = WavUtility.FromAudioClip(clip);
            string path = $"C:/GitHub/Npyu002/Npyu002Unity/{OutputDir}/{assetName}.wav";
            File.WriteAllBytes(path, bytes);
        }

        static void WireAudioManager()
        {
            var amGO = GameObject.Find("AudioManager");
            if (amGO == null) { Debug.LogWarning("[AudioGenerator] AudioManager GO not found."); return; }
            var am = amGO.GetComponent<ActionGame.AudioManager>();
            if (am == null) return;

            var so = new SerializedObject(am);
            so.FindProperty("bgmClip").objectReferenceValue         = Load("BGM_Battle");
            so.FindProperty("attackClip").objectReferenceValue      = Load("SE_Attack");
            so.FindProperty("hitClip").objectReferenceValue         = Load("SE_Hit");
            so.FindProperty("enemyDeathClip").objectReferenceValue  = Load("SE_EnemyDeath");
            so.FindProperty("playerDeathClip").objectReferenceValue = Load("SE_PlayerDeath");
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(amGO);
        }

        static AudioClip Load(string name) =>
            AssetDatabase.LoadAssetAtPath<AudioClip>($"{OutputDir}/{name}.wav");
    }

    /// <summary>float[] → WAV バイト列変換 (16bit PCM)</summary>
    static class WavUtility
    {
        public static byte[] FromAudioClip(AudioClip clip)
        {
            int channels   = clip.channels;
            int sampleRate  = clip.frequency;
            int sampleCount = clip.samples * channels;
            var samples = new float[sampleCount];
            clip.GetData(samples, 0);

            using var ms = new System.IO.MemoryStream();
            using var bw = new System.IO.BinaryWriter(ms);

            int dataSize = sampleCount * 2;
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + dataSize);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16); bw.Write((short)1);
            bw.Write((short)channels); bw.Write(sampleRate);
            bw.Write(sampleRate * channels * 2);
            bw.Write((short)(channels * 2)); bw.Write((short)16);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(dataSize);
            foreach (var s in samples)
                bw.Write((short)Mathf.Clamp(s * 32767f, -32768f, 32767f));

            return ms.ToArray();
        }
    }
}
