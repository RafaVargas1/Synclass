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

  it('does not call onSelecionar when confirming without picking hour/minute (achado de dev-review, PR #120)', async () => {
    const onSelecionar = jest.fn();
    await render(<SeletorDeHora label="Início" valor={undefined} onSelecionar={onSelecionar} />);

    await fireEvent.press(screen.getByText('Selecionar hora'));
    await fireEvent.press(screen.getByText('Confirmar'));

    expect(onSelecionar).not.toHaveBeenCalled();
  });

  it('does not confirm with only the hour picked (minute still missing)', async () => {
    const onSelecionar = jest.fn();
    await render(<SeletorDeHora label="Início" valor={undefined} onSelecionar={onSelecionar} />);

    await fireEvent.press(screen.getByText('Selecionar hora'));
    await fireEvent.press(screen.getByLabelText('Hora 10'));
    await fireEvent.press(screen.getByText('Confirmar'));

    expect(onSelecionar).not.toHaveBeenCalled();
  });

  it('closes the panel without changing the value when pressed again while open (toggle, issue #113/#117)', async () => {
    const onSelecionar = jest.fn();
    await render(<SeletorDeHora label="Início" valor={undefined} onSelecionar={onSelecionar} />);

    await fireEvent.press(screen.getByText('Selecionar hora'));
    expect(screen.getByText('Hora')).toBeTruthy();

    await fireEvent.press(screen.getByText('Selecionar hora'));

    expect(screen.queryByText('Hora')).toBeNull();
    expect(onSelecionar).not.toHaveBeenCalled();
  });

  it('gives every hour and minute button a min touch target of 44 (issue #115)', async () => {
    await render(<SeletorDeHora label="Início" valor={undefined} onSelecionar={() => {}} />);

    await fireEvent.press(screen.getByText('Selecionar hora'));

    for (const hora of Horas) {
      const botao = screen.getByLabelText(`Hora ${hora}`);
      expect(botao.props.style).toEqual(
        expect.objectContaining({ minWidth: 44, minHeight: 44 }),
      );
    }
    for (const minuto of Minutos) {
      const botao = screen.getByLabelText(`Minuto ${minuto}`);
      expect(botao.props.style).toEqual(
        expect.objectContaining({ minWidth: 44, minHeight: 44 }),
      );
    }
  });
});
