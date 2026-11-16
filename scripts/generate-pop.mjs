import { writeFileSync } from 'node:fs';

const rate = 44100;
const count = Math.round(rate * 0.085);
const pcm = new Float64Array(count);
let phase = 0;
for (let i = 0; i < count; i++) {
  const t = i / rate;
  phase += 2 * Math.PI * (420 + 1300 * Math.exp(-t / 0.006)) / rate;
  const attack = 1 - Math.exp(-t / 0.0006);
  const tail = Math.min(1, (count - 1 - i) / (rate * 0.012));
  const body = Math.sin(phase) * Math.exp(-t / 0.012);
  const snap = 0.18 * Math.sin(2 * Math.PI * 3100 * t) * Math.exp(-t / 0.002);
  pcm[i] = attack * tail * (body + snap);
}
const peak = Math.max(...pcm.map(Math.abs));
const wav = Buffer.alloc(44 + count * 2);
wav.write('RIFF', 0);
wav.writeUInt32LE(wav.length - 8, 4);
wav.write('WAVEfmt ', 8);
wav.writeUInt32LE(16, 16);
wav.writeUInt16LE(1, 20);
wav.writeUInt16LE(1, 22);
wav.writeUInt32LE(rate, 24);
wav.writeUInt32LE(rate * 2, 28);
wav.writeUInt16LE(2, 32);
wav.writeUInt16LE(16, 34);
wav.write('data', 36);
wav.writeUInt32LE(count * 2, 40);
for (let i = 0; i < count; i++) wav.writeInt16LE(Math.round(pcm[i] / peak * 0.5 * 32767), 44 + i * 2);
writeFileSync(new URL('../Resources/pop.wav', import.meta.url), wav);
