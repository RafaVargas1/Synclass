import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { atualizarNome, buscarPerfil } from '@/lib/api/usuarios';
import { useSessao } from '@/lib/auth/contexto-sessao';

import PerfilScreen from './perfil';

const mockRouterReplace = jest.fn();
jest.mock('expo-router', () => ({
  useRouter: () => ({ replace: mockRouterReplace }),
}));

jest.mock('@/lib/auth/contexto-sessao', () => ({
  useSessao: jest.fn(),
}));

jest.mock('@/lib/api/usuarios', () => ({
  buscarPerfil: jest.fn(),
  atualizarNome: jest.fn(),
}));

const useSessaoMock = useSessao as jest.Mock;
const buscarPerfilMock = buscarPerfil as jest.Mock;
const atualizarNomeMock = atualizarNome as jest.Mock;

describe('PerfilScreen', () => {
  beforeEach(() => {
    mockRouterReplace.mockReset();
    useSessaoMock.mockReset();
    buscarPerfilMock.mockReset();
    atualizarNomeMock.mockReset();
    useSessaoMock.mockReturnValue({ carregando: false, token: 'token-jwt' });
  });

  it('redirects to /login when there is no saved session', async () => {
    useSessaoMock.mockReturnValue({ carregando: false, token: null });

    await render(<PerfilScreen />);

    await waitFor(() => expect(mockRouterReplace).toHaveBeenCalledWith('/login'));
  });

  it('shows the current nome after loading the perfil', async () => {
    buscarPerfilMock.mockResolvedValue({ sucesso: true, nome: 'Maria Silva' });

    await render(<PerfilScreen />);

    await waitFor(() => expect(screen.getByDisplayValue('Maria Silva')).toBeTruthy());
  });

  it('exposes "Meu perfil" as an accessible heading', async () => {
    buscarPerfilMock.mockResolvedValue({ sucesso: true, nome: 'Maria Silva' });

    await render(<PerfilScreen />);

    await waitFor(() =>
      expect(screen.getByRole('header', { name: 'Meu perfil' })).toBeTruthy(),
    );
  });

  it('does not overwrite an in-progress edit when the initial fetch resolves late', async () => {
    let resolverFetch: (resultado: { sucesso: true; nome: string }) => void = () => {};
    buscarPerfilMock.mockReturnValue(
      new Promise((resolve) => {
        resolverFetch = resolve;
      }),
    );
    await render(<PerfilScreen />);
    await fireEvent.changeText(screen.getByDisplayValue(''), 'Nome Digitado Pelo Usuário');

    resolverFetch({ sucesso: true, nome: 'Maria Silva' });

    await waitFor(() => expect(buscarPerfilMock).toHaveBeenCalled());
    expect(screen.getByDisplayValue('Nome Digitado Pelo Usuário')).toBeTruthy();
    expect(screen.queryByDisplayValue('Maria Silva')).toBeNull();
  });

  it('shows a success message and the updated nome when saving succeeds', async () => {
    buscarPerfilMock.mockResolvedValue({ sucesso: true, nome: 'Maria Silva' });
    atualizarNomeMock.mockResolvedValue({ sucesso: true, nome: 'Maria Souza' });
    await render(<PerfilScreen />);
    await waitFor(() => expect(screen.getByDisplayValue('Maria Silva')).toBeTruthy());

    await fireEvent.changeText(screen.getByDisplayValue('Maria Silva'), 'Maria Souza');
    await fireEvent.press(screen.getByText('Salvar'));

    await waitFor(() => expect(screen.getByText('Nome atualizado com sucesso.')).toBeTruthy());
    expect(atualizarNomeMock).toHaveBeenCalledWith('Maria Souza');
  });

  it('shows the Api error message without crashing when saving is rejected', async () => {
    buscarPerfilMock.mockResolvedValue({ sucesso: true, nome: 'Maria Silva' });
    atualizarNomeMock.mockResolvedValue({
      sucesso: false,
      mensagem: 'Nome inválido: "". Esperado um nome não vazio.',
    });
    await render(<PerfilScreen />);
    await waitFor(() => expect(screen.getByDisplayValue('Maria Silva')).toBeTruthy());

    await fireEvent.changeText(screen.getByDisplayValue('Maria Silva'), '');
    await fireEvent.press(screen.getByText('Salvar'));

    await waitFor(() =>
      expect(screen.getByText('Nome inválido: "". Esperado um nome não vazio.')).toBeTruthy(),
    );
  });
});
