import { Linking } from 'react-native';

import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';
import { useLocalSearchParams } from 'expo-router';

import { cadastrarAlunoProvisorio } from '@/lib/api/alunosProvisorios';
import { gerarCodigoEntradaTurma } from '@/lib/api/codigosEntradaTurma';
import { gerarConvite } from '@/lib/api/convites';

import AdicionarAlunoScreen from './adicionar';

// TopbarAutenticada (#77) monta o MenuNavegacao real, que já tem sua
// própria suíte. Mockado aqui pra manter este arquivo focado no contrato
// da própria tela.
jest.mock('@/components/organisms/TopbarAutenticada', () => {
  const { View } = jest.requireActual('react-native');
  return {
    TopbarAutenticada: ({ children }: { children?: React.ReactNode }) => <View>{children}</View>,
  };
});

jest.mock('expo-router', () => ({
  useLocalSearchParams: jest.fn(),
  useRouter: () => ({ back: jest.fn(), replace: jest.fn(), canGoBack: () => false }),
}));

jest.mock('@/lib/api/alunosProvisorios', () => ({
  cadastrarAlunoProvisorio: jest.fn(),
}));

jest.mock('@/lib/api/convites', () => ({
  gerarConvite: jest.fn(),
}));

jest.mock('@/lib/api/codigosEntradaTurma', () => ({
  gerarCodigoEntradaTurma: jest.fn(),
}));

const useLocalSearchParamsMock = useLocalSearchParams as jest.Mock;
const cadastrarAlunoProvisorioMock = cadastrarAlunoProvisorio as jest.Mock;
const gerarConviteMock = gerarConvite as jest.Mock;
const gerarCodigoEntradaTurmaMock = gerarCodigoEntradaTurma as jest.Mock;

describe('AdicionarAlunoScreen', () => {
  beforeEach(() => {
    useLocalSearchParamsMock.mockReturnValue({ professorId: 'professor-1' });
    cadastrarAlunoProvisorioMock.mockReset();
    gerarConviteMock.mockReset();
    gerarCodigoEntradaTurmaMock.mockReset();
    jest.spyOn(Linking, 'openURL').mockResolvedValue(true);
  });

  it('shows the "Sem contato" tab by default, with the cadastro form', async () => {
    await render(<AdicionarAlunoScreen />);

    expect(screen.getByPlaceholderText('Nome do Aluno')).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Sem contato', selected: true })).toBeTruthy();
  });

  it('opens directly on the "Por convite" tab when matriculaId is present in the route', async () => {
    useLocalSearchParamsMock.mockReturnValue({ professorId: 'professor-1', matriculaId: 'matricula-1' });
    await render(<AdicionarAlunoScreen />);

    expect(screen.getByRole('button', { name: 'Por convite', selected: true })).toBeTruthy();
    expect(screen.getByPlaceholderText('E-mail ou telefone')).toBeTruthy();
  });

  it('switches to "Por convite" and submits gerarConvite with professorId from the route', async () => {
    gerarConviteMock.mockResolvedValue({
      sucesso: true,
      conviteId: 'convite-1',
      token: 'token-1',
      codigo: '12345',
      expiraEm: '2026-08-20T00:00:00Z',
    });
    await render(<AdicionarAlunoScreen />);

    await fireEvent.press(screen.getByText('Por convite'));
    await fireEvent.changeText(screen.getByPlaceholderText('E-mail ou telefone'), '11987654321');
    await fireEvent.press(screen.getByText('Gerar convite'));

    await waitFor(() => expect(screen.getByText('Convite gerado!')).toBeTruthy());
    expect(gerarConviteMock).toHaveBeenCalledWith({
      professorId: 'professor-1',
      contato: '(11) 98765-4321',
      matriculaId: undefined,
    });
  });

  it('switches to "Código da turma", generates a code and shows the countdown', async () => {
    gerarCodigoEntradaTurmaMock.mockResolvedValue({
      sucesso: true,
      codigo: '54321',
      expiraEm: new Date(Date.now() + 5 * 60 * 1000).toISOString(),
    });
    await render(<AdicionarAlunoScreen />);

    await fireEvent.press(screen.getByText('Código da turma'));
    await fireEvent.press(screen.getByText('Gerar código'));

    await waitFor(() => expect(screen.getByText('54321')).toBeTruthy());
    expect(gerarCodigoEntradaTurmaMock).toHaveBeenCalledWith('professor-1');
    expect(screen.getByText(/Expira em/)).toBeTruthy();
  });

  it('shows the Api error message when gerarCodigoEntradaTurma fails', async () => {
    gerarCodigoEntradaTurmaMock.mockResolvedValue({ sucesso: false, mensagem: 'Professor não encontrado.' });
    await render(<AdicionarAlunoScreen />);

    await fireEvent.press(screen.getByText('Código da turma'));
    await fireEvent.press(screen.getByText('Gerar código'));

    await waitFor(() => expect(screen.getByText('Professor não encontrado.')).toBeTruthy());
  });

  it('submits cadastrarAlunoProvisorio on the "Sem contato" tab', async () => {
    cadastrarAlunoProvisorioMock.mockResolvedValue({ sucesso: true, nome: 'João Pedro', identificador: 'ALU-4F2A' });
    await render(<AdicionarAlunoScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('Nome do Aluno'), 'João Pedro');
    await fireEvent.press(screen.getByText('Cadastrar'));

    await waitFor(() => expect(screen.getByText('Aluno provisório cadastrado!')).toBeTruthy());
    expect(cadastrarAlunoProvisorioMock).toHaveBeenCalledWith({ nome: 'João Pedro' });
  });
});
