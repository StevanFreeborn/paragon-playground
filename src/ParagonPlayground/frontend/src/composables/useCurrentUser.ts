import { ref } from 'vue';
import { me, type UserResponse } from '../services/auth';

const currentUser = ref<UserResponse | null>(null);

export function useCurrentUser() {
  async function fetchCurrentUser(): Promise<UserResponse | null> {
    try {
      currentUser.value = await me();
    } catch {
      currentUser.value = null;
    }

    return currentUser.value;
  }

  function clearCurrentUser() {
    currentUser.value = null;
  }

  return { currentUser, fetchCurrentUser, clearCurrentUser };
}
