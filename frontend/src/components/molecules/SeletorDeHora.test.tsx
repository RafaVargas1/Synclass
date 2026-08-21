import { fireEvent, render, screen } from '@testing-library/react-native';

import { SeletorDeHora } from './SeletorDeHora';

const Horas = Array.from({ length: 24 }, (_, i) => String(i).padStart(2, '0'));
const Minutos = Array.from({ length: 60 }, (_, i) => String(i).padStart(2, '0'));

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

  it('opens the panel on press listing hours 00-23 and minutes 00-59', async () => {
    await render(<SeletorDeHora label="Início" valor={undefined} onSelecionar={() => {}} />);

    await fireEvent.press(screen.getByText('Selecionar hora'));

    expect(screen.getByText('Hora')).toBeTruthy();
    expect(screen.getByText('Minuto')).toBeTruthy();
    for (const hora of Horas) {
      expect(screen.getByLabelText(`Hora ${hora}`)).toBeTruthy();
    }
    for (const minuto of Minutos) {
      expect(screen.getByLabelText(`Minuto ${minuto}`)).toBeTruthy();
    }
  });

  it('selects hour and minute, calls onSelecionar with HH:mm and closes the panel', async () => {
    const onSelecionar = jest.fn();
    await render(<SeletorDeHora label="Início" valor={undefined} onSelecionar={onSelecionar} />);

    await fireEvent.press(screen.getByText('Selecionar hora'));
    await fireEvent.press(screen.getByLabelText('Hora 10'));
    await fireEvent.press(screen.getByLabelText('Minuto 30'));
    await fireEvent.press(screen.getByText('Confirmar'));

    expect(onSelecionar).toHaveBeenCalledWith('10:30');
    expect(screen.queryByText('Confirmar')).toBeNull();
  });
});
