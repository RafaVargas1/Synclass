import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { cadastrarAlunoProvisorio } from '@/lib/api/alunosProvisorios';

import CadastroAlunoProvisorioScreen from './cadastro';

// TopbarAutenticada (#77) monta o MenuNavegacao real, que já tem sua
// própria suíte (MenuNavegacao.test.tsx). Mockado aqui pra manter este
// arquivo focado no contrato da própria tela, sem precisar mockar
// usePathname/useIsTelaLarga/usePerfilLogado só por causa do menu.
jest.mock('@/components/organisms/TopbarAutenticada', () => {
  const { View } = jest.requireActual('react-native');
  return {
    TopbarAutenticada: ({ children }: { children?: React.ReactNode }) => <View>{children}</View>,
  };
});

jest.mock('expo-router', () => ({
  useRouter: () => ({ back: jest.fn(), replace: jest.fn(), canGoBack: () => false }),
}));

jest.mock('@/lib/api/alunosProvisorios', () => ({
  cadastrarAlunoProvisorio: jest.fn(),
}));

const cadastrarAlunoProvisorioMock = cadastrarAlunoProvisorio as jest.Mock;

describe('CadastroAlunoProvisorioScreen', () => {
  beforeEach(() => {
    cadastrarAlunoProvisorioMock.mockReset();
  });

  it('shows an inline confirmation with the system-generated identifier when the Api responds with success', async () => {
    cadastrarAlunoProvisorioMock.mockResolvedValue({
      sucesso: true,
      nome: 'João Pedro',
      identificador: 'ALU-4F2A',
    });
    await render(<CadastroAlunoProvisorioScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('Nome do Aluno'), 'João Pedro');
    await fireEvent.press(screen.getByText('Cadastrar'));

    await waitFor(() => expect(screen.getByText('Aluno provisório cadastrado!')).toBeTruthy());
    expect(cadastrarAlunoProvisorioMock).toHaveBeenCalledWith({ nome: 'João Pedro' });
    expect(screen.getByText('ALU-4F2A')).toBeTruthy();
  });

  it('shows the Api error message without crashing when the Api rejects the cadastro', async () => {
    cadastrarAlunoProvisorioMock.mockResolvedValue({
      sucesso: false,
      mensagem: 'Não foi possível concluir a operação. Tente novamente.',
    });
    await render(<CadastroAlunoProvisorioScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('Nome do Aluno'), 'João Pedro');
    await fireEvent.press(screen.getByText('Cadastrar'));

    await waitFor(() =>
      expect(
        screen.getByText('Não foi possível concluir a operação. Tente novamente.'),
      ).toBeTruthy(),
    );
    expect(screen.getByText('Cadastrar')).toBeTruthy();
  });
});
