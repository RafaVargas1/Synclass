import { render, screen } from '@testing-library/react-native';

import { SeletorDeHora } from './SeletorDeHora';

describe('SeletorDeHora', () => {
  it('shows the value formatted as HH:mm when a value is provided', async () => {
    await render(<SeletorDeHora label="Início" valor="10:30" onSelecionar={() => {}} />);

    expect(screen.getByText('Início')).toBeTruthy();
    expect(screen.getByText('10:30')).toBeTruthy();
  });

  it('shows a placeholder when the value is undefined', async () => {
    await render(<SeletorDeHora label="Início" valor={undefined} onSelecionar={() => {}} />);

    expect(screen.getByText('Início')).toBeTruthy();
    expect(screen.getByText('Selecionar hora')).toBeTruthy();
  });
});
