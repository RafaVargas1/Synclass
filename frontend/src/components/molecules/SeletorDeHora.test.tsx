import { fireEvent, render, screen } from '@testing-library/react-native';

import { horaEstaCompleta } from '@/lib/mascararHora';

import { SeletorDeHora } from './SeletorDeHora';

describe('SeletorDeHora', () => {
  it('mostra o label do campo', async () => {
    await render(<SeletorDeHora label="Início" valor={undefined} onSelecionar={() => {}} />);

    expect(screen.getByText('Início')).toBeTruthy();
  });

  it('mostra o valor fornecido no campo (value do Input)', async () => {
    await render(<SeletorDeHora label="Início" valor="10:30" onSelecionar={() => {}} />);

    expect(screen.getByDisplayValue('10:30')).toBeTruthy();
  });

  it('chama onSelecionar com HH:mm ao digitar os 4 dígitos completos', async () => {
    const onSelecionar = jest.fn();
    await render(<SeletorDeHora label="Início" valor={undefined} onSelecionar={onSelecionar} />);

    await fireEvent.changeText(screen.getByLabelText('Início'), '1030');

    expect(onSelecionar).toHaveBeenLastCalledWith('10:30');
    expect(horaEstaCompleta(onSelecionar.mock.calls[onSelecionar.mock.calls.length - 1][0])).toBe(true);
  });

  it('não reporta como escolhido um valor parcial (nunca passa em horaEstaCompleta)', async () => {
    const onSelecionar = jest.fn();
    await render(<SeletorDeHora label="Início" valor={undefined} onSelecionar={onSelecionar} />);

    await fireEvent.changeText(screen.getByLabelText('Início'), '10');

    const chamadas = onSelecionar.mock.calls;
    expect(chamadas.length).toBeGreaterThan(0);
    const ultima = chamadas[chamadas.length - 1][0];
    expect(horaEstaCompleta(ultima)).toBe(false);
  });

  it('expoe o parâmetro accessibilityLabel como o label recebido', async () => {
    await render(<SeletorDeHora label="Início" valor={undefined} onSelecionar={() => {}} />);

    expect(screen.getByLabelText('Início')).toBeTruthy();
  });
});
