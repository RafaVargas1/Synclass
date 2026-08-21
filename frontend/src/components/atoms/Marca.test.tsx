import { render, screen } from '@testing-library/react-native';

import { Marca } from './Marca';

const AlturasBase = [12, 24, 16, 28, 18];

describe('Marca', () => {
  it('renders 5 bars with the base heights when escala is not given', async () => {
    await render(<Marca />);

    AlturasBase.forEach((altura, indice) => {
      expect(screen.getByTestId(`marca-barra-${indice}`)).toHaveStyle({ height: altura });
    });
  });

  it('applies escala as a multiplier over the base heights', async () => {
    await render(<Marca escala={0.5} />);

    AlturasBase.forEach((altura, indice) => {
      expect(screen.getByTestId(`marca-barra-${indice}`)).toHaveStyle({ height: altura * 0.5 });
    });
  });

  it('applies a vertical flip when invertido is given', async () => {
    await render(<Marca invertido />);

    expect(screen.getByTestId('marca')).toHaveStyle({ transform: [{ scaleY: -1 }] });
  });

  it('only uses the primary color tokens, no gold/bronze tone', async () => {
    await render(<Marca />);

    AlturasBase.forEach((_, indice) => {
      expect(screen.getByTestId(`marca-barra-${indice}`).props.className).toBe(
        'w-1.5 bg-primary dark:bg-dark-primary',
      );
    });
  });
});
