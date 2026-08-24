import { fireEvent, render, screen } from '@testing-library/react-native';

import { ModalConfirmacao } from './ModalConfirmacao';

describe('ModalConfirmacao', () => {
  it('não renderiza o conteúdo quando visivel é false', async () => {
    await render(
      <ModalConfirmacao
        visivel={false}
        titulo="Confirmar marcação"
        mensagem="Marcar o horário de Terça às 10:00?"
        onConfirmar={jest.fn()}
        onFechar={jest.fn()}
      />,
    );

    expect(screen.queryByText('Confirmar marcação')).toBeNull();
  });

  it('mostra título, mensagem e rótulos padrão quando visivel', async () => {
    await render(
      <ModalConfirmacao
        visivel
        titulo="Confirmar marcação"
        mensagem="Marcar o horário de Terça às 10:00?"
        onConfirmar={jest.fn()}
        onFechar={jest.fn()}
      />,
    );

    expect(screen.getByText('Confirmar marcação')).toBeTruthy();
    expect(screen.getByText('Marcar o horário de Terça às 10:00?')).toBeTruthy();
    expect(screen.getByText('Confirmar')).toBeTruthy();
    expect(screen.getByText('Cancelar')).toBeTruthy();
  });

  it('aceita rótulos customizados de confirmar/cancelar', async () => {
    await render(
      <ModalConfirmacao
        visivel
        titulo="Confirmar cancelamento"
        mensagem="Cancelar a aula de Terça às 10:00?"
        rotuloConfirmar="Cancelar aula"
        rotuloCancelar="Voltar"
        onConfirmar={jest.fn()}
        onFechar={jest.fn()}
      />,
    );

    expect(screen.getByText('Cancelar aula')).toBeTruthy();
    expect(screen.getByText('Voltar')).toBeTruthy();
  });

  it('chama onConfirmar ao tocar no botão de confirmar', async () => {
    const onConfirmar = jest.fn();
    await render(
      <ModalConfirmacao
        visivel
        titulo="Confirmar marcação"
        mensagem="Marcar?"
        onConfirmar={onConfirmar}
        onFechar={jest.fn()}
      />,
    );

    await fireEvent.press(screen.getByText('Confirmar'));

    expect(onConfirmar).toHaveBeenCalled();
  });

  it('chama onFechar ao tocar no botão de cancelar', async () => {
    const onFechar = jest.fn();
    await render(
      <ModalConfirmacao
        visivel
        titulo="Confirmar marcação"
        mensagem="Marcar?"
        onConfirmar={jest.fn()}
        onFechar={onFechar}
      />,
    );

    await fireEvent.press(screen.getByText('Cancelar'));

    expect(onFechar).toHaveBeenCalled();
  });

  it('chama onFechar ao tocar no fundo (fora do card)', async () => {
    const onFechar = jest.fn();
    await render(
      <ModalConfirmacao
        visivel
        titulo="Confirmar marcação"
        mensagem="Marcar?"
        onConfirmar={jest.fn()}
        onFechar={onFechar}
      />,
    );

    await fireEvent.press(screen.getByTestId('modal-confirmacao-backdrop'));

    expect(onFechar).toHaveBeenCalled();
  });

  it('não fecha ao tocar dentro do card (evita fechar sem querer)', async () => {
    const onFechar = jest.fn();
    await render(
      <ModalConfirmacao
        visivel
        titulo="Confirmar marcação"
        mensagem="Marcar?"
        onConfirmar={jest.fn()}
        onFechar={onFechar}
      />,
    );

    await fireEvent.press(screen.getByTestId('modal-confirmacao'));

    expect(onFechar).not.toHaveBeenCalled();
  });

  it('dá aos botões um alvo de toque de ao menos 44x44 (issue #115)', async () => {
    await render(
      <ModalConfirmacao
        visivel
        titulo="Confirmar marcação"
        mensagem="Marcar?"
        onConfirmar={jest.fn()}
        onFechar={jest.fn()}
      />,
    );

    expect(screen.getByRole('button', { name: 'Confirmar' })).toHaveStyle({ minWidth: 44, minHeight: 44 });
    expect(screen.getByRole('button', { name: 'Cancelar' })).toHaveStyle({ minWidth: 44, minHeight: 44 });
  });
});
