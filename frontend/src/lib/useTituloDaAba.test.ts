import { act, renderHook } from '@testing-library/react-native';
import { useNavigation } from 'expo-router';

import { useTituloDaAba } from './useTituloDaAba';

jest.mock('expo-router', () => ({
  useNavigation: jest.fn(),
}));

const useNavigationMock = useNavigation as jest.Mock;
const setOptionsMock = jest.fn();

describe('useTituloDaAba', () => {
  beforeEach(() => {
    setOptionsMock.mockReset();
    useNavigationMock.mockReset();
    useNavigationMock.mockReturnValue({ setOptions: setOptionsMock });
  });

  it('chama setOptions com o título dado', async () => {
    await renderHook(() => useTituloDaAba('Valor devido'));

    expect(setOptionsMock).toHaveBeenCalledWith({ title: 'Valor devido' });
  });

  it('chama setOptions de novo quando o título muda', async () => {
    const { rerender } = await renderHook(({ titulo }: { titulo: string }) => useTituloDaAba(titulo), {
      initialProps: { titulo: 'Perfil' },
    });

    expect(setOptionsMock).toHaveBeenCalledWith({ title: 'Perfil' });

    await act(async () => {
      rerender({ titulo: 'Horários' });
    });

    expect(setOptionsMock).toHaveBeenCalledWith({ title: 'Horários' });
    expect(setOptionsMock).toHaveBeenCalledTimes(2);
  });
});
