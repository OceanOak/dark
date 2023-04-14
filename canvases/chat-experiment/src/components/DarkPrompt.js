import React, {useEffect, useState}from 'react'

const DarkPrompt = () => {

  const [prompt, setPrompt] = useState('');
  const [item, setItem] = useState({ open: 0 });

  useEffect(() => {
    const fetchPrompt = async () => {
      try {
        const response = await fetch('/get-prompt');
        const data = await response.text();
        setPrompt(data);
      } catch (error) {
        console.error(error);
      }
    };
    fetchPrompt();
  }, []);


  const toggle = () => {
    const open = item.open === 1 ? 0 : 1;
    setItem({ ...item, open });
  };

  return (
    <div className="mx-4 text-white">
    <div className={`bg-[#3C3C3C] p-5 border-none rounded-md w-full relative ${item.open === 1 ? 'is-open' : ''}`} >
      <div className='flex items-center'>
        <div className='w-full font-bold'>System prompt</div>
        <div className='text-xl cursor-pointer' onMouseDown={toggle}> + </div>
      </div>
      <div className={`overflow-hidden ${item.open === 1 ? 'max-h-full' : 'max-h-0'}`}>
        <textarea className='w-full border-none bg-transparent p-0 pl-3 m-0 outline-none' id="dark-prompt" defaultValue="this is dark prompt" />
      </div>
    </div>
    </div>
  );


};

export default DarkPrompt;
