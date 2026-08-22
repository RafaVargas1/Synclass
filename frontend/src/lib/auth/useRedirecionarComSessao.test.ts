import { renderHook, waitFor } from '@testing-library/react-native';

import { useRedirecionarComSessao } from '@/lib/auth/useRedirecionarComSessao';

const mockRouterReplace = jest.fn();
jest.mock('expo-router', () => ({
  useRouter: () => ({ replace: mockRouterReplace }),
}));

describe('useRedirecionarComSessao', () => {
  beforeEach(() => {
    mockRouterReplace.mockReset();
  });

  it('redirects to /painel when there is a saved session', async () => {
    renderHook(() => useRedirecionarComSessao(false, 'token-jwt'));

    await waitFor(() => expect(mockRouterReplace).toHaveBeenCalledWith('/painel'));
  });

  it('does not redirect while the saved session is still loading', () => {
    renderHook(() => useRedirecionarComSessao(true, 'token-jwt'));

    expect(mockRouterReplace).not.toHaveBeenCalled();
  });

  it('does not redirect when there is no saved session', () => {
    renderHook(() => useRedirecionarComSessao(false, null));

    expect(mockRouterReplace).not.toHaveBeenCalled();
  });
});
