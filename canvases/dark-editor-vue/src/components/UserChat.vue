<template>
<div class="text-white bg-slate-500 rounded-md flex flex-col stretch my-4 mx-auto max-w-3xl shadow-lg shadow-black/10">
      <p>{{ promptValue }}</p>

</div>
</template>

<script setup lang="ts">
import { defineProps } from 'vue';

const props = defineProps({
  promptValue: {
    type: String,
    required: true
  }
});
</script>
