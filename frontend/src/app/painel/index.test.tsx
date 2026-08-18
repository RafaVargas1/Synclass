import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { useSessao } from '@/lib/auth/contexto-sessao';

import PainelScreen from './index';

const mockRouterReplace = jest.fn();
jest.mock('expo-router', () => ({
  useRouter: () => ({ replace: mockRouterReplace }),
}));

jest.mock('@/lib/auth/contexto-sessao', () => ({
  useSessao: jest.fn(),
}));

const useSessaoMock = useSessao as jest.Mock;

describe('PainelScreen', () => {
  beforeEach(() => {
    mockRouterReplace.mockReset();
    useSessaoMock.mockReset();
  });

  it('redirects to /login when there is no saved session', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: null,
      papeis: [],
      papelAtivo: undefined,
      definirPapelAtivo: jest.fn(),
    });

    await render(<PainelScreen />);

    await waitFor(() => expect(mockRouterReplace).toHaveBeenCalledWith('/login'));
  });

  it('does not redirect while the saved session is still loading', async () => {
    useSessaoMock.mockReturnValue({
      carregando: true,
      token: null,
      papeis: [],
      papelAtivo: undefined,
      definirPapelAtivo: jest.fn(),
    });

    await render(<PainelScreen />);

    expect(mockRouterReplace).not.toHaveBeenCalled();
  });

  it('shows AlternadorDePapel only when there is more than one papel', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papeis: ['Professor', 'Aluno'],
      papelAtivo: 'Professor',
      definirPapelAtivo: jest.fn(),
    });

    await render(<PainelScreen />);

    expect(screen.getByRole('button', { name: 'Professor', selected: true })).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Aluno', selected: false })).toBeTruthy();
  });

  it('does not show AlternadorDePapel with a single papel', async () => {
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papeis: ['Professor'],
      papelAtivo: 'Professor',
      definirPapelAtivo: jest.fn(),
    });

    await render(<PainelScreen />);

    expect(screen.queryByRole('tablist')).toBeNull();
  });

  it('shows the actions for the active papel and switches them when the papel changes', async () => {
    const definirPapelAtivo = jest.fn();
    useSessaoMock.mockReturnValue({
      carregando: false,
      token: 'token-jwt',
      papeis: ['Professor', 'Aluno'],
      papelAtivo: 'Professor',
      definirPapelAtivo,
    });

    await render(<PainelScreen />);

    expect(screen.getByText('Gerenciar horários')).toBeTruthy();
    expect(screen.queryByText('Marcar aula em horário vago')).toBeNull();

    await fireEvent.press(screen.getByText('Aluno'));

    expect(definirPapelAtivo).toHaveBeenCalledWith('Aluno');
  });
});
