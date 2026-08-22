import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { listarValorDevido } from '@/lib/api/valorDevido';
import { buscarPerfil } from '@/lib/api/usuarios';
import { useSessao } from '@/lib/auth/contexto-sessao';
import { periodoDoDia } from '@/lib/periodoDoDia';

import PainelScreen from './index';

const mockRouterReplace = jest.fn();
jest.mock('expo-router', () => {
  const { Text } = jest.requireActual('react-native');
  return {
    useRouter: () => ({ replace: mockRouterReplace }),
    Link: ({ href, children }: { href: string; children: React.ReactNode }) => (
      <Text testID={`link-${href}`}>{children}</Text>
    ),
  };
});

// TopbarAutenticada (#77) monta o MenuNavegacao real, que já tem sua
// própria suíte (MenuNavegacao.test.tsx e TopbarAutenticada.test.tsx).
// Mockado aqui pra manter este arquivo focado no contrato do próprio
// Painel (conteúdo do corpo).
jest.mock('@/components/organisms/TopbarAutenticada', () => {
  const { View } = jest.requireActual('react-native');
  return {
    TopbarAutenticada: ({ children }: { children?: React.ReactNode }) => <View>{children}</View>,
  };
});

jest.mock('@/lib/auth/contexto-sessao', () => ({
  useSessao: jest.fn(),
}));

jest.mock('@/lib/api/usuarios', () => ({
  buscarPerfil: jest.fn(),
}));

jest.mock('@/lib/api/valorDevido', () => ({
  listarValorDevido: jest.fn(),
}));

jest.mock('@/lib/periodoDoDia', () => {
  const actual = jest.requireActual('@/lib/periodoDoDia');
  return { ...actual, periodoDoDia: jest.fn() };
});

const useSessaoMock = useSessao as jest.Mock;
const buscarPerfilMock = buscarPerfil as jest.Mock;
const listarValorDevidoMock = listarValorDevido as jest.Mock;
const periodoDoDiaMock = periodoDoDia as jest.Mock;

describe('PainelScreen', () => {
  beforeEach(() => {
    mockRouterReplace.mockReset();
    useSessaoMock.mockReset();
    buscarPerfilMock.mockReset();
    buscarPerfilMock.mockResolvedValue({ sucesso: false, mensagem: 'erro' });
    periodoDoDiaMock.mockReturnValue('manha');
  });

  it('redirects to /login when there is no saved session', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: null,
      sair: jest.fn(),
    });

    await render(<PainelScreen />);

    await waitFor(() => expect(mockRouterReplace).toHaveBeenCalledWith('/login'));
  });

  it('does not redirect while the saved session is still loading', async () => {
    useSessaoMock.mockReturnValue({
      carregando: true,
      token: null,
      sair: jest.fn(),
    });

    await render(<PainelScreen />);

    expect(mockRouterReplace).not.toHaveBeenCalled();
  });

  it('shows an action card for each seção of the papel ativo, not a lone "Meu perfil" CTA', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papelAtivo: 'Aluno',
      sair: jest.fn(),
    });

    await render(<PainelScreen />);

    expect(screen.getByTestId('link-/aluno/historico-frequencia')).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Ver histórico de frequência' })).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Ver valor devido' })).toBeTruthy();
    expect(screen.queryByRole('button', { name: 'Meu perfil' })).toBeNull();
  });

  it('calls sair when the Sair button is pressed', async () => {
    const sair = jest.fn();
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      sair,
    });

    await render(<PainelScreen />);
    await fireEvent.press(screen.getByText('Sair'));

    expect(sair).toHaveBeenCalled();
  });

  it('gives the Sair button a touch target of at least 44x44 (issue #115)', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      sair: jest.fn(),
    });

    await render(<PainelScreen />);

    expect(screen.getByRole('button', { name: 'Sair' })).toHaveStyle({
      minWidth: 44,
      minHeight: 44,
    });
  });
});

describe('PainelScreen saudação', () => {
  beforeEach(() => {
    mockRouterReplace.mockReset();
    useSessaoMock.mockReset();
    buscarPerfilMock.mockReset();
    buscarPerfilMock.mockResolvedValue({ sucesso: true, usuarioId: 'prof-1', nome: 'Ana' });
    useSessaoMock.mockReturnValue({ carregando: false, token: 'token-jwt', sair: jest.fn() });
  });

  it.each([
    ['manha', 'Bom dia'],
    ['tarde', 'Boa tarde'],
    ['noite', 'Boa noite'],
  ])('exibe "{saudacao}, Ana" para o período %s', async (periodo, saudacao) => {
    periodoDoDiaMock.mockReturnValue(periodo);

    await render(<PainelScreen />);

    await waitFor(() => expect(screen.getByText(`${saudacao}, Ana`)).toBeTruthy());
  });

  it('não mostra saudação antes do perfil resolver', async () => {
    buscarPerfilMock.mockReturnValue(new Promise(() => {}));

    await render(<PainelScreen />);

    expect(screen.queryByText(/^(Bom dia|Boa tarde|Boa noite),/)).toBeNull();
  });
});

describe('PainelScreen resumo de valor a receber', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    periodoDoDiaMock.mockReturnValue('manha');
    buscarPerfilMock.mockResolvedValue({ sucesso: true, usuarioId: 'prof-1', nome: 'Ana' });
    listarValorDevidoMock.mockResolvedValue({ sucesso: true, valoresDevidos: [] });
  });

  it('mostra o total somado dos Alunos no mês e o rótulo "A receber este mês"', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papelAtivo: 'Professor',
      sair: jest.fn(),
    });
    listarValorDevidoMock.mockResolvedValue({
      sucesso: true,
      valoresDevidos: [
        { matriculaId: 'm1', alunoUsuarioId: 'a1', nome: 'Bia', valor: 300, semRegraDefinida: false },
        { matriculaId: 'm2', alunoUsuarioId: 'a2', nome: 'Caio', valor: 450, semRegraDefinida: false },
      ],
    });

    await render(<PainelScreen />);

    await waitFor(() => expect(screen.getByText('A receber este mês')).toBeTruthy());
    expect(screen.getByText('R$ 750,00')).toBeTruthy();
  });

  it('mostra a mensagem de valor zerado sem valor em destaque quando a lista é vazia', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papelAtivo: 'Professor',
      sair: jest.fn(),
    });
    listarValorDevidoMock.mockResolvedValue({ sucesso: true, valoresDevidos: [] });

    await render(<PainelScreen />);

    await waitFor(() => expect(screen.getByText('Nenhum valor a receber neste mês.')).toBeTruthy());
    expect(screen.queryByText('R$ 0,00')).toBeNull();
  });

  it('mostra a mesma mensagem de valor zerado quando todos os Alunos estão sem regra definida', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papelAtivo: 'Professor',
      sair: jest.fn(),
    });
    listarValorDevidoMock.mockResolvedValue({
      sucesso: true,
      valoresDevidos: [{ matriculaId: 'm1', alunoUsuarioId: 'a1', nome: 'Bia', valor: null, semRegraDefinida: true }],
    });

    await render(<PainelScreen />);

    await waitFor(() => expect(screen.getByText('Nenhum valor a receber neste mês.')).toBeTruthy());
    expect(screen.queryByText(/^R\$/)).toBeNull();
  });

  it('chama listarValorDevido com o usuarioId do Professor sem query string de período', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papelAtivo: 'Professor',
      sair: jest.fn(),
    });

    await render(<PainelScreen />);

    await waitFor(() => expect(listarValorDevidoMock).toHaveBeenCalledWith('prof-1'));
    expect(listarValorDevidoMock).toHaveBeenCalledTimes(1);
    expect(listarValorDevidoMock.mock.calls[0][1]).toBeUndefined();
  });

  it('não mostra a seção "a receber" para o papel ativo Aluno', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papelAtivo: 'Aluno',
      sair: jest.fn(),
    });

    await render(<PainelScreen />);

    await waitFor(() => expect(screen.getByTestId('link-/aluno/historico-frequencia')).toBeTruthy());
    expect(screen.queryByText('A receber este mês')).toBeNull();
    expect(listarValorDevidoMock).not.toHaveBeenCalled();
  });
});
