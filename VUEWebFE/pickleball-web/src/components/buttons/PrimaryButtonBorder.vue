<template>
  <component
    :is="isRouterLink ? 'router-link' : 'button'"
    :to="isRouterLink ? to : undefined"
    :replace="isRouterLink && replace ? replace : undefined"
    :class="['p-pri-button']"
  >
    <slot name="name">Button</slot>
  </component>
</template>

<script>
export default {
  props: {
    isRouterLink: {
      type: Boolean,
      default: false, // Mặc định là không phải router-link
    },
    to: {
      type: [String, Object],
      default: null, // Chỉ sử dụng khi là router-link
    },
    isActive: {
      type: Boolean,
      default: false, // Nhận giá trị isActive từ parent
    },
    replace: {
      type: Boolean,
      default: false, // Mặc định không thay thế URL hiện tại
    },
  },
};
</script>

<style scoped>
.p-pri-button {
  display: flex;
  justify-content: center;
  align-items: center;
  border-radius: 100px;
  color: rgb(255, 255, 255);
  background-color: transparent;
  height: 36px;
  width: 100%;
  border: none;
  text-decoration: none;
  font-family: roboto-medium;
  font-size: 15px;
  backdrop-filter: blur(10px);
  border: 1px solid rgba(255, 255, 255, 1);
}
.p-pri-button:hover {
  background-color: rgba(255, 255, 255, 0.5);
  cursor: pointer;
}
.router-link-active {
  background-color: white;
  color: var(--topic-color-700);
}
</style>