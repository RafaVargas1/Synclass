import { render, screen } from '@testing-library/react-native';

import { CadastroConfirmado } from './CadastroConfirmado';

jest.mock('expo-router', () => {
  const { Text } = jest.requireActual('react-native');
  return {
    Link: ({ href, children }: { href: string; children: React.ReactNode }) => (
      <Text testID={`link-${href}`}>{children}</Text>
    ),
  };
});

describe('CadastroConfirmado', () => {
  it('shows the confirmation message for the given papel', async () => {
    await render(<CadastroConfirmado papel="Professor" />);

    expect(screen.getByText('Cadastro concluído!')).toBeTruthy();
    expect(screen.getByText('Seu cadastro como Professor foi realizado com sucesso.')).toBeTruthy();
  });

  it('shows a link to /login, so the user is not left on a dead-end screen (issue #143)', async () => {
    await render(<CadastroConfirmado papel="Aluno" />);

    expect(screen.getByTestId('link-/login')).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Entrar' })).toBeTruthy();
  });
});
