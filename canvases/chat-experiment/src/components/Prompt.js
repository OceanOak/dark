import React from "react";

const Prompt = () => {
  return (
    <div className="flex flex-col absolute left-0 right-0 bottom-0 my-4 mx-12 ">
      <div className="w-full bg-[#3C3C3C] border-none rounded">
        <textarea className="w-full text-white border-none bg-transparent p-0 pl-3 m-0 outline-none " />
      </div>
      <p className="mx-auto mt-3 text-xs text-[#6e6d6d]">
        For details about how we collect and use your data, please refer to our
        privacy policy
      </p>
    </div>
  );
};


export default Prompt;