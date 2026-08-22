import { fireEvent, render, screen } from '@testing-library/react-native';

import { Notificacao } from './Notificacao';

describe('Notificacao', () => {
  it.each([
    ['erro', 'Erro'],
    ['aviso', 'Aviso'],
    ['informacao', 'Informação'],
    ['sucesso', 'Sucesso'],
  ] as const)('shows the rótulo "%s" → "%s" alongside the mensagem, never color alone', async (tipo, rotulo) => {
    await render(<Notificacao tipo={tipo} mensagem="Algo aconteceu" onFechar={jest.fn()} />);

    expect(screen.getByText(rotulo)).toBeTruthy();
    expect(screen.getByText('Algo aconteceu')).toBeTruthy();
  });

  it('is announced as an alert for screen readers', async () => {
    await render(<Notificacao tipo="erro" mensagem="Falha ao salvar" onFechar={jest.fn()} />);

    expect(screen.getByRole('alert')).toBeTruthy();
  });

  it('calls onFechar when the close button is pressed', async () => {
    const onFechar = jest.fn();
    await render(<Notificacao tipo="informacao" mensagem="Aviso qualquer" onFechar={onFechar} />);

    await fireEvent.press(screen.getByRole('button', { name: 'Fechar notificação' }));

    expect(onFechar).toHaveBeenCalled();
  });

  it('gives the close button a touch target of at least 44x44 (Fitts/HIG)', async () => {
    await render(<Notificacao tipo="erro" mensagem="Falha" onFechar={jest.fn()} />);

    expect(screen.getByRole('button', { name: 'Fechar notificação' })).toHaveStyle({
      minWidth: 44,
      minHeight: 44,
    });
  });
});
