import { Colors } from './palette.js';
import { razaoDeContraste } from './contraste';

describe('paleta clara', () => {
  const { background, backgroundElement, backgroundSelected } = Colors.light;

  it('mantém a escada de elevação: razão de contraste entre background, backgroundElement e backgroundSelected >= 1.10 par a par', () => {
    expect(razaoDeContraste(background, backgroundElement)).toBeGreaterThanOrEqual(1.1);
    expect(razaoDeContraste(background, backgroundSelected)).toBeGreaterThanOrEqual(1.1);
    expect(razaoDeContraste(backgroundElement, backgroundSelected)).toBeGreaterThanOrEqual(1.1);
  });

  it('atende AA (4.5:1) para texto normal sobre o background do modo claro', () => {
    const { text, textSecondary } = Colors.light;
    expect(razaoDeContraste(text, background)).toBeGreaterThanOrEqual(4.5);
    expect(razaoDeContraste(textSecondary, background)).toBeGreaterThanOrEqual(4.5);
  });
});
