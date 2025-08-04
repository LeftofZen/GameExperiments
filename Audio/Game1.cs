using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Shared;
using System;

namespace Audio
{
	public enum WaveType
	{
		Sine,
		Square,
		Triangle
	}

	public enum RainSurfaceType
	{
		Concrete,
		Water,
		Grass,
		Metal
	}

	public class Game1 : ImGuiGame
	{
		private SoundEffect _squareWave;

		public Game1()
		{
			Content.RootDirectory = "Content";
			IsMouseVisible = true;
		}

		protected override void Initialize()
		{
			base.Initialize();
		}

		protected override void LoadContent()
		{ }

		protected override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.SteelBlue);
			base.Draw(gameTime);
		}

		private float rainIntensity = 300f;
		private float minDropSpeed = 3.0f;
		private float maxDropSpeed = 9.0f;
		private float rainVolume = 0.5f;
		private int rainDuration = 2;
		private RainSurfaceType rainSurface = RainSurfaceType.Concrete;
		private SoundEffect rainSound;

		protected override void DrawImGui(GameTime gameTime)
		{
			if (ImGui.Begin("Waveform Generator"))
			{
				// Add ImGui Combo to select wave type
				var waveTypes = Enum.GetNames(typeof(WaveType));
				var currentWave = (int)waveType;
				if (ImGui.Combo("Wave Type", ref currentWave, waveTypes, waveTypes.Length))
				{
					waveType = (WaveType)currentWave;
				}

				if (ImGui.Button("Generate Wave"))
				{
					GenerateAndPlayWave();
				}
			}
			ImGui.End();

			if (ImGui.Begin("Rain Synthesizer"))
			{
				ImGui.SliderInt("Duration (s)", ref rainDuration, 1, 10);
				ImGui.SliderFloat("Rain Intensity (drops/sec)", ref rainIntensity, 10f, 2000f);
				ImGui.SliderFloat("Min Drop Speed (m/s)", ref minDropSpeed, 1.0f, 10.0f);
				ImGui.SliderFloat("Max Drop Speed (m/s)", ref maxDropSpeed, minDropSpeed, 15.0f);
				ImGui.SliderFloat("Volume", ref rainVolume, 0.01f, 1.0f);

				var surfaceTypes = Enum.GetNames(typeof(RainSurfaceType));
				var currentSurface = (int)rainSurface;
				if (ImGui.Combo("Surface", ref currentSurface, surfaceTypes, surfaceTypes.Length))
				{
					rainSurface = (RainSurfaceType)currentSurface;
				}

				if (ImGui.Button("Play Rain"))
				{
					var buffer = SynthesizeRain(
						durationSeconds: rainDuration,
						rainIntensity: rainIntensity,
						minDropSpeed: minDropSpeed,
						maxDropSpeed: maxDropSpeed,
						volume: rainVolume,
						surface: rainSurface
					);
					rainSound = new SoundEffect(buffer, 44100, AudioChannels.Mono);
					rainSound.Play();
				}

				if (ImGui.Button("Play Physical Rain"))
				{
					var buffer = SynthesizePhysicalRain(
						durationSeconds: rainDuration,
						rainIntensity: rainIntensity,
						minDropSpeed: minDropSpeed,
						maxDropSpeed: maxDropSpeed,
						volume: rainVolume,
						surface: rainSurface
					);
					rainSound = new SoundEffect(buffer, 44100, AudioChannels.Mono);
					rainSound.Play();
				}
			}
			ImGui.End();
		}

		private WaveType waveType = WaveType.Sine;

		private void GenerateAndPlayWave()
		{
			var sampleRate = 44100;
			var durationSeconds = 1;
			var frequency = 440;
			short amplitude = 16000;
			var sampleCount = sampleRate * durationSeconds;
			var buffer = new byte[sampleCount * 2]; // 16-bit audio

			for (var i = 0; i < sampleCount; i++)
			{
				short sample = 0;
				var t = (double)i / sampleRate;

				switch (waveType)
				{
					case WaveType.Square:
						sample = (short)((i % (sampleRate / frequency) < (sampleRate / frequency) / 2) ? amplitude : -amplitude);
						break;
					case WaveType.Sine:
						sample = (short)(amplitude * Math.Sin(2 * Math.PI * frequency * t));
						break;
					case WaveType.Triangle:
						sample = (short)(2 * amplitude / Math.PI * Math.Asin(Math.Sin(2 * Math.PI * frequency * t)));
						break;
				}

				buffer[i * 2] = (byte)(sample & 0xFF);
				buffer[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
			}

			_squareWave = new SoundEffect(buffer, sampleRate, AudioChannels.Mono);
			_squareWave.Play();
		}

		public static byte[] SynthesizeRain(
			int durationSeconds = 2,
			int sampleRate = 44100,
			float rainIntensity = 300f, // drops per second
			float minDropSpeed = 3.0f,  // m/s
			float maxDropSpeed = 9.0f,  // m/s
			float volume = 0.5f,        // 0..1
			RainSurfaceType surface = RainSurfaceType.Concrete)
		{
			int totalSamples = durationSeconds * sampleRate;
			float[] buffer = new float[totalSamples];
			Random rng = new();

			// Surface properties (affecting filter and envelope)
			float baseFreq, decay, brightness;
			switch (surface)
			{
				case RainSurfaceType.Water: baseFreq = 600f; decay = 0.10f; brightness = 0.7f; break;
				case RainSurfaceType.Grass: baseFreq = 400f; decay = 0.08f; brightness = 0.4f; break;
				case RainSurfaceType.Metal: baseFreq = 2000f; decay = 0.15f; brightness = 1.0f; break;
				default: // Concrete
					baseFreq = 1200f; decay = 0.12f; brightness = 0.8f; break;
			}

			// Poisson process for raindrop impacts
			double dropInterval = 1.0 / rainIntensity;
			double t = 0;
			while (t < durationSeconds)
			{
				// Drop time in samples
				int dropSample = (int)(t * sampleRate);

				// Drop properties
				float dropSpeed = (float)(minDropSpeed + rng.NextDouble() * (maxDropSpeed - minDropSpeed));
				float dropSize = (float)(Math.Pow(rng.NextDouble(), 2.0) * 0.002 + 0.001); // smaller drops more likely
				float impactEnergy = dropSize * dropSpeed * (0.5f + 0.5f * (float)rng.NextDouble());

				// Envelope
				int envLen = (int)(decay * sampleRate * (0.7f + 0.6f * (float)rng.NextDouble()));
				for (int i = 0; i < envLen && (dropSample + i) < totalSamples; i++)
				{
					// Exponential decay envelope
					float env = (float)Math.Exp(-3.0 * i / envLen);

					// White noise burst, filtered (simple 1-pole lowpass)
					float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
					float freq = baseFreq * (0.7f + 0.6f * dropSpeed / maxDropSpeed) * (1.0f + brightness * (float)rng.NextDouble());
					float alpha = freq / sampleRate;
					if (alpha > 1f) alpha = 1f;
					// Simple lowpass filter (leaky integrator)
					float prev = (i > 0) ? buffer[dropSample + i - 1] : 0f;
					float filtered = prev + alpha * (noise - prev);

					buffer[dropSample + i] += filtered * env * impactEnergy * volume;
				}

				// Next drop (Poisson process)
				t += -Math.Log(1.0 - rng.NextDouble()) * dropInterval;
			}

			// Normalize and convert to 16-bit PCM
			float max = 1e-6f;
			foreach (var s in buffer) max = Math.Max(max, Math.Abs(s));
			short[] pcm = new short[totalSamples];
			for (int i = 0; i < totalSamples; i++)
				pcm[i] = (short)Math.Clamp(buffer[i] / max * 32767f, short.MinValue, short.MaxValue);

			// Convert to byte[]
			byte[] bytes = new byte[pcm.Length * 2];
			Buffer.BlockCopy(pcm, 0, bytes, 0, bytes.Length);
			return bytes;
		}

		public static byte[] SynthesizePhysicalRain(
	int durationSeconds = 2,
	int sampleRate = 44100,
	float rainIntensity = 400f, // drops per second
	float minDropSpeed = 3.0f,  // m/s
	float maxDropSpeed = 9.0f,  // m/s
	float volume = 0.5f,        // 0..1
	RainSurfaceType surface = RainSurfaceType.Concrete)
		{
			int totalSamples = durationSeconds * sampleRate;
			float[] buffer = new float[totalSamples];
			Random rng = new();

			// Surface resonance properties (tuned for realism)
			float minFreq, maxFreq, minDecay, maxDecay, minNoiseDecay, maxNoiseDecay, noiseMix;
			switch (surface)
			{
				case RainSurfaceType.Water: minFreq = 2500f; maxFreq = 6000f; minDecay = 0.012f; maxDecay = 0.025f; minNoiseDecay = 0.003f; maxNoiseDecay = 0.008f; noiseMix = 0.7f; break;
				case RainSurfaceType.Grass: minFreq = 1200f; maxFreq = 3000f; minDecay = 0.015f; maxDecay = 0.030f; minNoiseDecay = 0.004f; maxNoiseDecay = 0.010f; noiseMix = 0.8f; break;
				case RainSurfaceType.Metal: minFreq = 4000f; maxFreq = 9000f; minDecay = 0.018f; maxDecay = 0.035f; minNoiseDecay = 0.002f; maxNoiseDecay = 0.006f; noiseMix = 0.5f; break;
				default: // Concrete
					minFreq = 3000f; maxFreq = 7000f; minDecay = 0.014f; maxDecay = 0.028f; minNoiseDecay = 0.003f; maxNoiseDecay = 0.009f; noiseMix = 0.6f; break;
			}

			double dropInterval = 1.0 / rainIntensity;
			double t = 0;
			while (t < durationSeconds)
			{
				int dropSample = (int)(t * sampleRate);

				// Drop properties
				float dropSpeed = (float)(minDropSpeed + rng.NextDouble() * (maxDropSpeed - minDropSpeed));
				float dropSize = (float)(Math.Pow(rng.NextDouble(), 2.0) * 0.002 + 0.001);

				// Frequency and decay for this drop
				float freq = minFreq + (float)rng.NextDouble() * (maxFreq - minFreq);
				float decay = minDecay + (float)rng.NextDouble() * (maxDecay - minDecay);
				float noiseDecay = minNoiseDecay + (float)rng.NextDouble() * (maxNoiseDecay - minNoiseDecay);

				// Amplitude: much lower for realism!
				float amp = dropSize * dropSpeed * (0.01f + 0.01f * (float)rng.NextDouble()) * volume;

				// Synthesize the decaying sine burst + noise burst
				int envLen = (int)(decay * sampleRate);
				int noiseLen = (int)(noiseDecay * sampleRate);
				double phase = 0;
				double phaseInc = 2 * Math.PI * freq / sampleRate;
				for (int i = 0; i < envLen && (dropSample + i) < totalSamples; i++)
				{
					float env = (float)Math.Exp(-3.0 * i / envLen);
					float sine = (float)(Math.Sin(phase) * env * amp * (1.0f - noiseMix));
					phase += phaseInc;

					float noise = 0f;
					if (i < noiseLen)
					{
						float noiseEnv = (float)Math.Exp(-4.0 * i / noiseLen); // faster decay for noise
																			   // High-frequency noise: difference of two randoms (whitens and centers)
						noise = ((float)rng.NextDouble() - (float)rng.NextDouble()) * noiseEnv * amp * noiseMix;
					}

					buffer[dropSample + i] += sine + noise;
				}

				// Next drop (Poisson process)
				t += -Math.Log(1.0 - rng.NextDouble()) * dropInterval;
			}

			// Normalize and convert to 16-bit PCM
			float max = 1e-6f;
			foreach (var s in buffer) max = Math.Max(max, Math.Abs(s));
			short[] pcm = new short[totalSamples];
			for (int i = 0; i < totalSamples; i++)
				pcm[i] = (short)Math.Clamp(buffer[i] / max * 32767f, short.MinValue, short.MaxValue);

			byte[] bytes = new byte[pcm.Length * 2];
			Buffer.BlockCopy(pcm, 0, bytes, 0, bytes.Length);
			return bytes;
		}
	}
}
