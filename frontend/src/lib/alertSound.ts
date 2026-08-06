/**
 * Story 12.2 (T12.2.5): a short two-tone beep for "new order arrived." Generated via Web Audio
 * rather than shipping an audio asset — there's no sound file in the design package (the staff
 * area isn't part of it at all), and a couple of oscillator tones is simpler than sourcing/
 * licensing a real chime for two internal-only screens.
 */
export function playNewOrderAlert(): void {
  try {
    const context = new AudioContext();
    const now = context.currentTime;

    [880, 1175].forEach((frequency, index) => {
      const oscillator = context.createOscillator();
      const gain = context.createGain();
      oscillator.type = 'sine';
      oscillator.frequency.value = frequency;

      const start = now + index * 0.15;
      gain.gain.setValueAtTime(0, start);
      gain.gain.linearRampToValueAtTime(0.3, start + 0.02);
      gain.gain.exponentialRampToValueAtTime(0.001, start + 0.18);

      oscillator.connect(gain).connect(context.destination);
      oscillator.start(start);
      oscillator.stop(start + 0.2);
    });

    setTimeout(() => context.close(), 500);
  } catch {
    // Audio isn't essential to the kitchen board working — a browser blocking autoplay/
    // AudioContext shouldn't break the rest of the page.
  }
}
