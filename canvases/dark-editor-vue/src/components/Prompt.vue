<template>
     <form @submit.prevent="submitPrompt" class="flex flex-row stretch my-4 mx-auto max-w-2xl">
      <div class="relative flex h-full flex-1">
        <div class="w-full relative flex flex-col flex-grow py-3 pl-4 border border-white/10 rounded-md shadow-black/10">
          <textarea v-model="prompt" aria-multiline="true" rows="1" placeholder="What are you building today?" class="w-full h-6 max-h-48 outline-none m-0 resize-none overflow-y-auto border-0 bg-transparent text-white py-0 pl-2 pr-7 "></textarea>
          <button type="submit" class="absolute bottom-2 right-2 py-1 px-2  mx-2 rounded-md text-white hover:bg-white/5">send</button>
        </div>
      </div>
    </form>

    <div v-if="showPrompt">
      <UserChat :promptValue="prompt" />
    </div>

</template>
<script setup lang="ts">
import {ref} from 'vue';
import type UserChat from './UserChat.vue';
const prompt = ref('')
const showPrompt = ref(false)
const submitPrompt = () =>{
  console.log(prompt.value)
  showPrompt.value = true
}
</script>


