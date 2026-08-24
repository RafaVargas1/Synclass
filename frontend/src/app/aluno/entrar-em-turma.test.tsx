import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { aceitarCodigoEntradaTurma } from '@/lib/api/codigosEntradaTurma';
import { aceitarConvitePorCodigo } from '@/lib/api/convites';
import { useSessao } from '@/lib/auth/contexto-sessao';
import { usePerfilLogado } from '@/lib/usePerfilLogado';

import EntrarEmNovaTurmaScreen from './entrar-em-turma';

// TopbarAutenticada (#77) monta o MenuNavegacao real, que já tem sua
// própria suíte. Mockado aqui pra manter este arquivo focado no contrato
// da própria tela, mesmo padrão de professor/alunos/adicionar.test.tsx.
jest.mock('@/components/organisms/TopbarAutenticada', () => {
  const { View } = jest.requireActual('react-native');
  return {
    TopbarAutenticada: ({ children }: { children?: React.ReactNode }) => <View>{children}</View>,
  };
});

jest.mock('@/lib/auth/contexto-sessao', () => ({
  useSessao: jest.fn(),
}));

jest.mock('@/lib/usePerfilLogado', () => ({
  usePerfilLogado: jest.fn(),
}));

jest.mock('@/lib/api/convites', () => ({
  aceitarConvitePorCodigo: jest.fn(),
}));

jest.mock('@/lib/api/codigosEntradaTurma', () => ({
  aceitarCodigoEntradaTurma: jest.fn(),
}));

const mockUseSessao = useSessao as jest.Mock;
const mockUsePerfilLogado = usePerfilLogado as jest.Mock;
const mockAceitarConvitePorCodigo = aceitarConvitePorCodigo as jest.Mock;
const mockAceitarCodigoEntradaTurma = aceitarCodigoEntradaTurma as jest.Mock;

describe('EntrarEmNovaTurmaScreen', () => {
  beforeEach(() => {
    mockUseSessao.mockReset();
    mockUsePerfilLogado.mockReset();
    mockAceitarConvitePorCodigo.mockReset();
    mockAceitarCodigoEntradaTurma.mockReset();
    mockUseSessao.mockReturnValue({ token: 'token-jwt' });
    mockUsePerfilLogado.mockReturnValue({
      usuarioId: 'usuario-1',
      nome: 'Ana Souza',
      contato: 'ana@example.com',
      erro: false,
      tentarNovamente: jest.fn(),
    });
    // Fallback padrão: código de entrada de turma rejeita, cai pro convite —
    // sobrescrito nos testes que exercitam o caminho de sucesso do código de
    // entrada.
    mockAceitarCodigoEntradaTurma.mockResolvedValue({ sucesso: false, mensagem: 'Código inválido ou expirado.' });
  });

  it('does not render any nome/contato field — only código', async () => {
    await render(<EntrarEmNovaTurmaScreen />);

    expect(screen.getByPlaceholderText('00000')).toBeTruthy();
    expect(screen.queryByPlaceholderText('Nome')).toBeNull();
    expect(screen.queryByPlaceholderText(/contato/i)).toBeNull();
  });

  it('submits the código with nome/contato from the logged Aluno profile, without asking again', async () => {
    mockAceitarConvitePorCodigo.mockResolvedValue({ sucesso: true, usuarioId: 'usuario-1', nome: 'Ana Souza', papeis: ['Aluno'] });
    await render(<EntrarEmNovaTurmaScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('00000'), '12345');
    await fireEvent.press(screen.getByText('Entrar na turma'));

    await waitFor(() => expect(screen.getByText('Turma adicionada!')).toBeTruthy());
    expect(mockAceitarConvitePorCodigo).toHaveBeenCalledWith({
      codigo: '12345',
      nome: 'Ana Souza',
      contato: 'ana@example.com',
    });
  });

  it('shows the Api error message without crashing when the Api rejects the código', async () => {
    mockAceitarConvitePorCodigo.mockResolvedValue({
      sucesso: false,
      mensagem: 'Código inválido ou expirado.',
    });
    await render(<EntrarEmNovaTurmaScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('00000'), '12345');
    await fireEvent.press(screen.getByText('Entrar na turma'));

    await waitFor(() => expect(screen.getByText('Código inválido ou expirado.')).toBeTruthy());
    expect(screen.getByText('Entrar na turma')).toBeTruthy();
  });

  it('shows an error and does not call the Api when the profile has not resolved yet', async () => {
    mockUsePerfilLogado.mockReturnValue({
      usuarioId: undefined,
      nome: undefined,
      contato: undefined,
      erro: false,
      tentarNovamente: jest.fn(),
    });
    await render(<EntrarEmNovaTurmaScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('00000'), '12345');
    await fireEvent.press(screen.getByText('Entrar na turma'));

    expect(mockAceitarConvitePorCodigo).not.toHaveBeenCalled();
    expect(screen.getByText('Não foi possível confirmar seu perfil. Tente novamente em instantes.')).toBeTruthy();
  });

  it('succeeds via the código de entrada de turma without ever calling aceitarConvitePorCodigo', async () => {
    mockAceitarCodigoEntradaTurma.mockResolvedValue({
      sucesso: true,
      professorId: 'professor-1',
      professorNome: 'Prof. Ana',
    });
    await render(<EntrarEmNovaTurmaScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('00000'), '54321');
    await fireEvent.press(screen.getByText('Entrar na turma'));

    await waitFor(() => expect(screen.getByText('Turma adicionada!')).toBeTruthy());
    expect(mockAceitarCodigoEntradaTurma).toHaveBeenCalledWith('54321');
    expect(mockAceitarConvitePorCodigo).not.toHaveBeenCalled();
  });

  it('falls back to aceitarConvitePorCodigo when the código de entrada de turma rejects', async () => {
    mockAceitarCodigoEntradaTurma.mockResolvedValue({ sucesso: false, mensagem: 'Código inválido ou expirado.' });
    mockAceitarConvitePorCodigo.mockResolvedValue({ sucesso: true, usuarioId: 'usuario-1', nome: 'Ana Souza', papeis: ['Aluno'] });
    await render(<EntrarEmNovaTurmaScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('00000'), '12345');
    await fireEvent.press(screen.getByText('Entrar na turma'));

    await waitFor(() => expect(screen.getByText('Turma adicionada!')).toBeTruthy());
    expect(mockAceitarCodigoEntradaTurma).toHaveBeenCalledWith('12345');
    expect(mockAceitarConvitePorCodigo).toHaveBeenCalledWith({
      codigo: '12345',
      nome: 'Ana Souza',
      contato: 'ana@example.com',
    });
  });

  it('shows the convite error message when both schemes reject', async () => {
    mockAceitarCodigoEntradaTurma.mockResolvedValue({ sucesso: false, mensagem: 'Código inválido ou expirado.' });
    mockAceitarConvitePorCodigo.mockResolvedValue({ sucesso: false, mensagem: 'Convite inválido ou já utilizado.' });
    await render(<EntrarEmNovaTurmaScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('00000'), '12345');
    await fireEvent.press(screen.getByText('Entrar na turma'));

    await waitFor(() => expect(screen.getByText('Convite inválido ou já utilizado.')).toBeTruthy());
  });
});
