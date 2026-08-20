import { Colors } from './palette.js';
import { razaoDeContraste } from './contraste';

describe('paleta clara', () => {
  const { background, backgroundElement, backgroundSelected } = Colors.light;

  it('mantém a escada de elevação: razão de contraste entre background, backgroundElement e backgroundSelected >= 1.10 par a par', () => {
    expect(razaoDeContraste(background, backgroundElement)).toBeGreaterThanOrEqual(1.1);
    expect(razaoDeContraste(background, backgroundSelected)).toBeGreaterThanOrEqual(1.1);
    expect(razaoDeContraste(backgroundElement, backgroundSelected)).toBeGreaterThanOrEqual(1.1);
  });
});
